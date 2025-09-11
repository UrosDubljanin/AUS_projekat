using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read holding registers functions/requests.
    /// </summary>
    public class ReadHoldingRegistersFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadHoldingRegistersFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadHoldingRegistersFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            ModbusReadCommandParameters paramCom = this.CommandParameters as ModbusReadCommandParameters;

            byte[] request = new byte[12];

            Buffer.BlockCopy((Array)BitConverter.GetBytes(
                IPAddress.HostToNetworkOrder(short)paramCom.TransactionId)), 0, (Array)request, 0, 2);

            Buffer.BlockCopy((Array)BitConverter.GetBytes(
               IPAddress.HostToNetworkOrder(short)paramCom.ProtocolId)), 0, (Array)request, 2, 2);

            Buffer.BlockCopy((Array)BitConverter.GetBytes(
               IPAddress.HostToNetworkOrder(short)paramCom.Length)), 0, (Array)request, 4, 2);

            request[6] = paramCom.UnitId;
            request[7] = paramCom.FunctionCode;

            Buffer.BlockCopy((Array)BitConverter.GetBytes(
               IPAddress.HostToNetworkOrder(short)paramCom.StartAddress)), 0, (Array)request, 8, 2);

            Buffer.BlockCopy((Array)BitConverter.GetBytes(
               IPAddress.HostToNetworkOrder(short)paramCom.Quantity)), 0, (Array)request, 10, 2);

            return request
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            ModbusReadCommandParameters paramCom = this.CommandParameters as ModbusReadCommandParameters;
           
            Dictionary<Tuple<PointType, ushort>, ushort> d=new Dictionary<Tuple<PointType, ushort>, ushort>();

            ushort address = ((ModbusReadCommandParameters)CommandParameters).StartAddress;

            for(int i = 0; i < response[8]/2; i++)
            {
                byte byte1 = response[8 + 1 + i * 2];
                byte byte2 = response[8 + 2 + i * 2];
                ushort value1 = BitConverter.ToUInt16(new byte[2] { byte2, byte1 }, 0);

                d.Add(new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, address), value1);
                address++;
            }

            return d;
        }
    }
}