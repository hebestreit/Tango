﻿using System;
using System.Reflection;
using CSM.Commands.Data.Internal;
using CSM.Helpers;
using CSM.Networking;
using LiteNetLib;
using NLog;

namespace CSM.Commands.Handler.Internal
{
    public class ConnectionRequestHandler : CommandHandler<ConnectionRequestCommand>
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public ConnectionRequestHandler()
        {
            TransactionCmd = false;
            RelayOnServer = false;
        }

        protected override void Handle(ConnectionRequestCommand command)
        {
        }

        public void HandleOnServer(ConnectionRequestCommand command, NetPeer peer)
        {
            _logger.Info("Received connection request.");
            // Check to see if the game versions match
            if (command.GameVersion != BuildConfig.applicationVersion)
            {
                _logger.Info($"Connection rejected: Game versions {command.GameVersion} (client) and {BuildConfig.applicationVersion} (server) differ.");
                Command.SendToClient(peer, new ConnectionResultCommand
                {
                    Success = false,
                    Reason = $"Client and server have different game versions. Client: {command.GameVersion}, Server: {BuildConfig.applicationVersion}."
                });
                return;
            }

            // Check to see if the mod version matches
            Version version = Assembly.GetAssembly(typeof(Client)).GetName().Version;
            string versionString = $"{version.Major}.{version.Minor}";

            if (command.ModVersion != versionString)
            {
                _logger.Info($"Connection rejected: Mod versions {command.ModVersion} (client) and {versionString} (server) differ.");
                Command.SendToClient(peer, new ConnectionResultCommand
                {
                    Success = false,
                    Reason = $"Client and server have different CSM Mod versions. Client: {command.ModVersion}, Server: {versionString}."
                });
                return;
            }

            // Check the client username to see if anyone on the server already have a username
            bool hasExistingPlayer = MultiplayerManager.Instance.PlayerList.Contains(command.Username);
            if (hasExistingPlayer)
            {
                _logger.Info($"Connection rejected: Username {command.Username} already in use.");
                Command.SendToClient(peer, new ConnectionResultCommand
                {
                    Success = false,
                    Reason = "This username is already in use."
                });
                return;
            }

            // Check the password to see if it matches (only if the server has provided a password).
            if (!string.IsNullOrEmpty(MultiplayerManager.Instance.CurrentServer.Config.Password))
            {
                if (command.Password != MultiplayerManager.Instance.CurrentServer.Config.Password)
                {
                    _logger.Warn("Connection rejected: Invalid password provided!"); 
                    Command.SendToClient(peer, new ConnectionResultCommand
                    {
                        Success = false,
                        Reason = "Invalid password for this server."
                    });
                    return;
                }
            }

            SteamHelper.DLC_BitMask dlcMask = DLCHelper.GetOwnedDLCs();
            // Check both client have the same DLCs enabled
            if (!command.DLCBitMask.Equals(dlcMask))
            {
                _logger.Info($"Connection rejected: DLC bit mask {command.DLCBitMask} (client) and {dlcMask} (server) differ.");
                Command.SendToClient(peer, new ConnectionResultCommand
                {
                    Success = false,
                    Reason = "DLCs don't match",
                    DLCBitMask = dlcMask
                });
                return;
            }

            // Add the new player as a connected player
            Player newPlayer = new Player(peer, command.Username);
            MultiplayerManager.Instance.CurrentServer.ConnectedPlayers[peer.Id] = newPlayer;

            // Get a serialized version of the server world to send to the player.
            if (command.RequestWorld)
            {
                // Get the world
                byte[] world = WorldManager.GetWorld();

                // Send the result command
                Command.SendToClient(peer, new ConnectionResultCommand
                {
                    Success = true,
                    ClientId = peer.Id,
                    World = world
                });
            }
            else
            {
                // Send the result command
                Command.SendToClient(peer, new ConnectionResultCommand
                {
                    Success = true,
                    ClientId = peer.Id
                });
            }

            MultiplayerManager.Instance.CurrentServer.HandlePlayerConnect(newPlayer);
        }
    }
}
