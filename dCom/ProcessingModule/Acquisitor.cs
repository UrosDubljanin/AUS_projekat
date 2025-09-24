using Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for periodic polling.
    /// </summary>
    public class Acquisitor : IDisposable
	{
		private AutoResetEvent acquisitionTrigger;
        private IProcessingManager processingManager;
        private Thread acquisitionWorker;
		private IStateUpdater stateUpdater;
		private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Acquisitor"/> class.
        /// </summary>
        /// <param name="acquisitionTrigger">The acquisition trigger.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="stateUpdater">The state updater.</param>
        /// <param name="configuration">The configuration.</param>
		public Acquisitor(AutoResetEvent acquisitionTrigger, IProcessingManager processingManager, IStateUpdater stateUpdater, IConfiguration configuration)
		{
			this.stateUpdater = stateUpdater;
			this.acquisitionTrigger = acquisitionTrigger;
			this.processingManager = processingManager;
			this.configuration = configuration;
			this.InitializeAcquisitionThread();
			this.StartAcquisitionThread();
		}

		#region Private Methods

        /// <summary>
        /// Initializes the acquisition thread.
        /// </summary>
		private void InitializeAcquisitionThread()
		{
			this.acquisitionWorker = new Thread(Acquisition_DoWork);
			this.acquisitionWorker.Name = "Acquisition thread";
		}

        /// <summary>
        /// Starts the acquisition thread.
        /// </summary>
		private void StartAcquisitionThread()
		{
			acquisitionWorker.Start();
		}

        /// <summary>
        /// Acquisitor thread logic.
        /// </summary>
		private void Acquisition_DoWork()
        {
            List<IConfigItem> config = configuration.GetConfigurationItems();

            // konstante iz zadatka
            int inflowStep = 80;     // 80 l/s po stepenu P1/P2
            int outflow = 50;        // 50 l/s
            int drainageLevel = 6000;
            int maxLevel = 12000;

            while (true)
            {
                acquisitionTrigger.WaitOne();

                // 1) standardno čitanje
                foreach (IConfigItem item in config)
                {
                    item.SecondsPassedSinceLastPoll++;
                    if (item.SecondsPassedSinceLastPoll == item.AcquisitionInterval)
                    {
                        processingManager.ExecuteReadCommand(item,
                                                             configuration.GetTransactionId(),
                                                             configuration.UnitAddress,
                                                             item.StartAddress,
                                                             item.NumberOfRegisters);
                        item.SecondsPassedSinceLastPoll = 0;
                    }
                }

                // 2) simulacija promene nivoa L
                // pronalazak config item-a za L, P1, P2, STOP, V1
                IConfigItem level = config.Find(x => x.Description == "L");
                IConfigItem stop = config.Find(x => x.Description == "STOP");
                IConfigItem p1 = config.Find(x => x.Description == "P1");
                IConfigItem p2 = config.Find(x => x.Description == "P2");
                IConfigItem v1 = config.Find(x => x.Description == "V1");

                if (level != null && stop != null && p1 != null && p2 != null && v1 != null)
                {
                    int currentLevel = (int)level.DefaultValue; // trenutno stanje L (možeš koristiti storage ako imaš)

                    if (stop.DefaultValue == 0) // STOP = 0 → pumpa radi
                    {
                        int inflow = 0;
                        if (p1.DefaultValue == 1 && p2.DefaultValue == 0) inflow = 160;
                        else if (p1.DefaultValue == 0 && p2.DefaultValue == 1) inflow = 80;
                        else if (p1.DefaultValue == 1 && p2.DefaultValue == 1) inflow = 240;

                        currentLevel += inflow;
                    }
                    else // STOP = 1 → ventil može da radi
                    {
                        if (v1.DefaultValue == 1 && currentLevel > drainageLevel)
                        {
                            currentLevel -= outflow;
                        }
                    }

                    // granice
                    if (currentLevel < 0) currentLevel = 0;
                    if (currentLevel > maxLevel) currentLevel = maxLevel;

                    // upis nove vrednosti u registar L (1000)
                    processingManager.ExecuteWriteCommand(level,
                                                          configuration.GetTransactionId(),
                                                          configuration.UnitAddress,
                                                          level.StartAddress,
                                                          currentLevel);
                }
            }
        }


        #endregion Private Methods

        /// <inheritdoc />
        public void Dispose()
		{
			acquisitionWorker.Abort();
        }
	}
}