using Common;
using System;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for automated work.
    /// </summary>
    public class AutomationManager : IAutomationManager, IDisposable
	{
		private Thread automationWorker;
        private AutoResetEvent automationTrigger;
        private IStorage storage;
		private IProcessingManager processingManager;
		private int delayBetweenCommands;
        private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationManager"/> class.
        /// </summary>
        /// <param name="storage">The storage.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="automationTrigger">The automation trigger.</param>
        /// <param name="configuration">The configuration.</param>
        public AutomationManager(IStorage storage, IProcessingManager processingManager, AutoResetEvent automationTrigger, IConfiguration configuration)
		{
			this.storage = storage;
			this.processingManager = processingManager;
            this.configuration = configuration;
            this.automationTrigger = automationTrigger;
        }

        /// <summary>
        /// Initializes and starts the threads.
        /// </summary>
		private void InitializeAndStartThreads()
		{
			InitializeAutomationWorkerThread();
			StartAutomationWorkerThread();
		}

        /// <summary>
        /// Initializes the automation worker thread.
        /// </summary>
		private void InitializeAutomationWorkerThread()
		{
			automationWorker = new Thread(AutomationWorker_DoWork);
			automationWorker.Name = "Aumation Thread";
		}

        /// <summary>
        /// Starts the automation worker thread.
        /// </summary>
		private void StartAutomationWorkerThread()
		{
			automationWorker.Start();
		}


        private void AutomationWorker_DoWork()
        {
            var config = configuration.GetConfigurationItems();

            IConfigItem stop = config.Find(x => x.Description == "STOP");
            IConfigItem p1 = config.Find(x => x.Description == "P1");
            IConfigItem p2 = config.Find(x => x.Description == "P2");
            IConfigItem v1 = config.Find(x => x.Description == "V1");
            IConfigItem level = config.Find(x => x.Description == "L");

            int highAlarm = (int)level.HighLimit; // iz cfg fajla (npr. 10500 L)

            while (!disposedValue)
            {
                automationTrigger.WaitOne();

                int currentLevel = level.DefaultValue;

                // --- AUTOMATSKO PRAŽNJENJE (HighAlarm) ---
                if (currentLevel >= highAlarm)
                {
                    // STOP=1
                    processingManager.ExecuteWriteCommand(stop,
                                                          configuration.GetTransactionId(),
                                                          configuration.UnitAddress,
                                                          stop.StartAddress,
                                                          1);

                    // pumpa OFF
                    processingManager.ExecuteWriteCommand(p1,
                                                          configuration.GetTransactionId(),
                                                          configuration.UnitAddress,
                                                          p1.StartAddress,
                                                          0);
                    processingManager.ExecuteWriteCommand(p2,
                                                          configuration.GetTransactionId(),
                                                          configuration.UnitAddress,
                                                          p2.StartAddress,
                                                          0);

                    // ventil ON
                    processingManager.ExecuteWriteCommand(v1,
                                                          configuration.GetTransactionId(),
                                                          configuration.UnitAddress,
                                                          v1.StartAddress,
                                                          1);
                }

                // --- INTERLOCK LOGIKA ZA STOP ---
                if (stop.DefaultValue == 1) // STOP=1
                {
                    // onemogući pumpu
                    if (p1.DefaultValue != 0)
                    {
                        processingManager.ExecuteWriteCommand(p1,
                                                              configuration.GetTransactionId(),
                                                              configuration.UnitAddress,
                                                              p1.StartAddress,
                                                              0);
                    }
                    if (p2.DefaultValue != 0)
                    {
                        processingManager.ExecuteWriteCommand(p2,
                                                              configuration.GetTransactionId(),
                                                              configuration.UnitAddress,
                                                              p2.StartAddress,
                                                              0);
                    }
                }
                else // STOP=0
                {
                    // onemogući ventil
                    if (v1.DefaultValue != 0)
                    {
                        processingManager.ExecuteWriteCommand(v1,
                                                              configuration.GetTransactionId(),
                                                              configuration.UnitAddress,
                                                              v1.StartAddress,
                                                              0);
                    }
                }

                Thread.Sleep(delayBetweenCommands); // kontrolisani ciklus
            }
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls


        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Indication if managed objects should be disposed.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
				}
				disposedValue = true;
			}
		}


		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// GC.SuppressFinalize(this);
		}

        /// <inheritdoc />
        public void Start(int delayBetweenCommands)
		{
			this.delayBetweenCommands = delayBetweenCommands*1000;
            InitializeAndStartThreads();
		}

        /// <inheritdoc />
        public void Stop()
		{
			Dispose();
		}
		#endregion
	}
}
