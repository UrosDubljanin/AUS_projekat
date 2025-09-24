using Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for automated work of the water tank
    /// (STOP interlock, automatic drainage).
    /// </summary>
    public class AutomationManager : IAutomationManager, IDisposable
    {
        private Thread automationWorker;
        private AutoResetEvent automationTrigger;
        private IStorage storage;
        private IProcessingManager processingManager;
        private int delayBetweenCommands;
        private IConfiguration configuration;

        private int previousLevel = -1;
        private EGUConverter eGUConverter = new EGUConverter();

        public AutomationManager(IStorage storage, IProcessingManager processingManager, AutoResetEvent automationTrigger, IConfiguration configuration)
        {
            this.storage = storage;
            this.processingManager = processingManager;
            this.configuration = configuration;
            this.automationTrigger = automationTrigger;
        }

        private void InitializeAndStartThreads()
        {
            InitializeAutomationWorkerThread();
            StartAutomationWorkerThread();
        }

        private void InitializeAutomationWorkerThread()
        {
            automationWorker = new Thread(AutomationWorker_DoWork);
            automationWorker.Name = "Automation Thread";
        }

        private void StartAutomationWorkerThread()
        {
            automationWorker.Start();
        }

        private void AutomationWorker_DoWork()
        {
            // tačke definisane u RtuCfg.txt
            List<PointIdentifier> pointList = new List<PointIdentifier>
            {
                //Tip tacke i adresa registra
                new PointIdentifier(PointType.DIGITAL_OUTPUT, 2000), // STOP
                new PointIdentifier(PointType.DIGITAL_OUTPUT, 2005), // P1
                new PointIdentifier(PointType.DIGITAL_OUTPUT, 2006), // P2
                new PointIdentifier(PointType.DIGITAL_OUTPUT, 2002), // V1
                new PointIdentifier(PointType.ANALOG_OUTPUT, 1000)   // L (nivo)
            };

            while (!disposedValue)
            {
                automationTrigger.WaitOne();

                //Dobijamo trenutne vrijednosti svih tacaka gore definisanih
                List<IPoint> points = storage.GetPoints(pointList);

                //ovdje ih mapiramo
                IDigitalPoint stop = points[0] as IDigitalPoint;
                IDigitalPoint p1 = points[1] as IDigitalPoint;
                IDigitalPoint p2 = points[2] as IDigitalPoint;
                IDigitalPoint v1 = points[3] as IDigitalPoint;
                IAnalogPoint level = points[4] as IAnalogPoint;

                // nivo u EGU jedinicama
                //Kada poredimo koristimo EGU vrijednosti
                //Kada upisujemo nazad u registre vracamo iz EGU u Raw
                int currentLevel = (int)eGUConverter.ConvertToEGU(
                    level.ConfigItem.ScaleFactor, //faktor skaliranja (A)
                    level.ConfigItem.Deviation,   //odstupanje (B)
                    level.RawValue                //sirova vrijednost iz registra
                );
                previousLevel = currentLevel;
                //Uzimamo vrijednost iz RtuCfg.txt 10500L
                int highAlarm = (int)level.ConfigItem.HighLimit;


                if (p1.RawValue == 1 && p2.RawValue==1)
                {
                    previousLevel += 240;
                }
                else if (p1.RawValue == 1 && p2.RawValue==0)
                {
                    previousLevel += 160;
                }else if(p1.RawValue ==0 && p2.RawValue == 1)
                {
                    previousLevel += 80;
                }

                if (v1.RawValue == 1 && previousLevel>=6000)
                {
                    previousLevel -= 50;
                }
                


                // --- AUTOMATSKO PRAŽNJENJE (HighAlarm) ---
                if (currentLevel >= highAlarm)
                {
                    //STO=1
                    processingManager.ExecuteWriteCommand(stop.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, stop.ConfigItem.StartAddress, 1);
                    //P1, P2->0
                    processingManager.ExecuteWriteCommand(p1.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, p1.ConfigItem.StartAddress, 0);
                    processingManager.ExecuteWriteCommand(p2.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, p2.ConfigItem.StartAddress, 0);
                    processingManager.ExecuteWriteCommand(v1.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, v1.ConfigItem.StartAddress, 1);
                }

                // --- INTERLOCK LOGIKA ZA STOP ---
                if (stop.RawValue == 1) // STOP=1 → pumpe OFF
                {
                    if (p1.RawValue != 0)
                        processingManager.ExecuteWriteCommand(p1.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, p1.ConfigItem.StartAddress, 0);

                    if (p2.RawValue != 0)
                        processingManager.ExecuteWriteCommand(p2.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, p2.ConfigItem.StartAddress, 0);
                }
                else // STOP=0 → ventil OFF
                {
                    if (v1.RawValue != 0)
                        processingManager.ExecuteWriteCommand(v1.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, v1.ConfigItem.StartAddress, 0);
                }

                // --- Upis nivoa ako se promijenio ---
                if (currentLevel != previousLevel)
                {
                    currentLevel = previousLevel;
                    //Radimo suprotno, da bi upisali u registar prebacujemo u RAW
                    int raw = (int)eGUConverter.ConvertToRaw(
                        level.ConfigItem.ScaleFactor,
                        level.ConfigItem.Deviation,
                        currentLevel
                    );

                    //Salje se nova vrijednost (u raw formatu) na adresi 1000
                    processingManager.ExecuteWriteCommand(level.ConfigItem,
                                                          configuration.GetTransactionId(),
                                                          configuration.UnitAddress,
                                                          level.ConfigItem.StartAddress,
                                                          raw);

                    previousLevel = currentLevel;
                }

                Thread.Sleep(delayBetweenCommands);
            }
        }

        #region IDisposable
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) { }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Start(int delayBetweenCommands)
        {
            this.delayBetweenCommands = delayBetweenCommands * 1000;
            InitializeAndStartThreads();
        }

        public void Stop()
        {
            Dispose();
        }
        #endregion
    }
}
