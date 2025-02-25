﻿using System;
using ProtoBuf;

namespace CSM.Commands.Data.Game
{
    /// <summary>
    ///     Contains generic information about the world. Sent after a client is connected to
    ///     ensure everything is in sync.
    /// </summary>
    /// Sent by:
    /// - WorldInfoHandler
    ///
    /// TODO: Remove when the whole map is synced on connect
    [ProtoContract]
    public class WorldInfoCommand : CommandBase
    {
        /// <summary>
        ///     The current day time hour. Set under SimulationManager.m_currentDayTimeHour.
        /// </summary>
        [ProtoMember(1)]
        public float CurrentDayTimeHour { get; set; }

        /// <summary>
        ///     The current game time. Set under SimulationManager.m_currentDayTime.
        /// </summary>
        [ProtoMember(2)]
        public DateTime CurrentGameTime { get; set; }
    }
}
