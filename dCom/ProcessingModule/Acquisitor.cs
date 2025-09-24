using Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Acquisitor: periodično čitanje tačaka i simulacija nivoa vode u rezervoaru.
    /// </summary>
    public class Acquisitor : IDisposable
    {
        private AutoResetEvent acquisitionTrigger;
        private IProcessingManager processingManager;
        private Thread acquisitionWorker;
        private IStateUpdater stateUpdater;
        private IConfiguration configuration;
        private IStorage storage;

        private EGUConverter eGUConverter = new EGUConverter();

        // konstante koje nisu u RtuCfg.txt
        private const int inflowStep = 80;      // 80 l/s po pumpi
        private const int outflow = 50;         // 50 l/s kad ventil otvori
        private const int drainageLevel = 6000; // 2m = 6000 L

        private int previousLevel = -1;

        public Acquisitor(AutoResetEvent acquisitionTrigger,
                          IProcessingManager processingManager,
                          IStateUpdater stateUpdater,
                          IConfiguration configuration,
                          IStorage storage)
        {
            this.stateUpdater = stateUpdater;
            this.acquisitionTrigger = acquisitionTrigger;
            this.processingManager = processingManager;
            this.configuration = configuration;
            this.storage = storage;

            this.InitializeAcquisitionThread();
            this.StartAcquisitionThread();
        }

        #region Private Methods

        private void InitializeAcquisitionThread()
        {
            this.acquisitionWorker = new Thread(Acquisition_DoWork);
            this.acquisitionWorker.Name = "Acquisition thread";
        }

        private void StartAcquisitionThread()
        {
            acquisitionWorker.Start();
        }

        private void Acquisition_DoWork()
        {
            List<IConfigItem> listaConfiga = configuration.GetConfigurationItems();

            // identifikatori ključnih tačaka
            List<PointIdentifier> pointList = new List<PointIdentifier>
            {
                new PointIdentifier(PointType.ANALOG_OUTPUT, 1000),  // L
                new PointIdentifier(PointType.DIGITAL_OUTPUT, 2000), // STOP
                new PointIdentifier(PointType.DIGITAL_OUTPUT, 2005), // P1
                new PointIdentifier(PointType.DIGITAL_OUTPUT, 2006), // P2
                new PointIdentifier(PointType.DIGITAL_OUTPUT, 2002)  // V1
            };

            while (true)
            {
                acquisitionTrigger.WaitOne();

                // 1) standardno periodično čitanje
                foreach (IConfigItem item in listaConfiga)
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

                // 2) simulacija promjene nivoa L
                List<IPoint> points = storage.GetPoints(pointList);

                IAnalogPoint level = points[0] as IAnalogPoint;
                IDigitalPoint stop = points[1] as IDigitalPoint;
                IDigitalPoint p1 = points[2] as IDigitalPoint;
                IDigitalPoint p2 = points[3] as IDigitalPoint;
                IDigitalPoint v1 = points[4] as IDigitalPoint;

                int currentLevel = (int)eGUConverter.ConvertToEGU(
                    level.ConfigItem.ScaleFactor,
                    level.ConfigItem.Deviation,
                    level.RawValue
                );

                int maxLevel = (int)level.ConfigItem.EGU_Max;

                if (stop.RawValue == 0) // STOP=0 → pumpe rade
                {
                    int inflow = 0;
                    if (p1.RawValue == 1 && p2.RawValue == 0) inflow = 2 * inflowStep;   // 160
                    else if (p1.RawValue == 0 && p2.RawValue == 1) inflow = inflowStep;  // 80
                    else if (p1.RawValue == 1 && p2.RawValue == 1) inflow = 3 * inflowStep; // 240

                    currentLevel += inflow;
                }
                else // STOP=1 → ventil može da radi
                {
                    if (v1.RawValue == 1 && currentLevel > drainageLevel)
                        currentLevel -= outflow;
                }

                // granice
                if (currentLevel < 0) currentLevel = 0;
                if (currentLevel > maxLevel) currentLevel = maxLevel;

                // 3) upis nove vrijednosti samo ako se promijenila
                if (currentLevel != previousLevel)
                {
                    int raw = (int)eGUConverter.ConvertToRaw(
                        level.ConfigItem.ScaleFactor,
                        level.ConfigItem.Deviation,
                        currentLevel
                    );

                    processingManager.ExecuteWriteCommand(level.ConfigItem,
                                                          configuration.GetTransactionId(),
                                                          configuration.UnitAddress,
                                                          level.ConfigItem.StartAddress,
                                                          raw);

                    previousLevel = currentLevel;
                }
            }
        }

        #endregion Private Methods

        public void Dispose()
        {
            acquisitionWorker.Abort();
        }
    }
}
