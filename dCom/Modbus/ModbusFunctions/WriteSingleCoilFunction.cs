using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write coil functions/requests.
    /// </summary>
    public class WriteSingleCoilFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleCoilFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleCoilFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            //TO DO: IMPLEMENT
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            Dictionary<Tuple<PointType, ushort>, ushort> values = new Directory<Tuple<PointType, ushort>, ushort>();

            byte[] tempAddress = new byte[2];
            byte[] tempValue = new byte[2];


            ushort address = BitConverter.ToUInt16(new byte[2] { response[9], response[8] }, 0);

            ushort value=BitConverter.ToUInt16(new byte[2] { response[11], response[10] }, 0);

            values.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, address), value);

            return values;
        }
    }
}