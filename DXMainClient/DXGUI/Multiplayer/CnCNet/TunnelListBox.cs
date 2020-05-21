﻿using DTAClient.Domain.Multiplayer.CnCNet;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A list box for listing CnCNet tunnel servers.
    /// </summary>
    class TunnelListBox : XNAMultiColumnListBox
    {
        public TunnelListBox(WindowManager windowManager, TunnelHandler tunnelHandler) : base(windowManager)
        {
            this.tunnelHandler = tunnelHandler;

            tunnelHandler.TunnelsRefreshed += TunnelHandler_TunnelsRefreshed;
            tunnelHandler.TunnelPinged += TunnelHandler_TunnelPinged;

            SelectedIndexChanged += TunnelListBox_SelectedIndexChanged;

            Width = 466;
            Height = 200;
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            AddColumn("Name", 230);
            AddColumn("Official", 70);
            AddColumn("Ping", 76);
            AddColumn("Players", 90);
            AllowRightClickUnselect = false;
            AllowKeyboardInput = true;
        }

        public event EventHandler ListRefreshed;

        private readonly TunnelHandler tunnelHandler;

        private int bestTunnelIndex = 0;
        private int lowestTunnelRating = int.MaxValue;

        private bool isManuallySelectedTunnel;
        private string manuallySelectedTunnelAddress;


        /// <summary>
        /// Selects a tunnel from the list with the given address.
        /// </summary>
        /// <param name="address">The address of the tunnel server to select.</param>
        public void SelectTunnel(string address)
        {
            int index = tunnelHandler.Tunnels.FindIndex(t => t.Address == address);
            if (index > -1)
            {
                SelectedIndex = index;
                isManuallySelectedTunnel = true;
                manuallySelectedTunnelAddress = address;
            }
        }

        private void TunnelHandler_TunnelsRefreshed(object sender, EventArgs e)
        {
            ClearItems();

            int tunnelIndex = 0;

            foreach (CnCNetTunnel tunnel in tunnelHandler.Tunnels)
            {
                List<string> info = new List<string>();

                info.Add(tunnel.Name);
                info.Add(Conversions.BooleanToString(tunnel.Official, BooleanStringStyle.YESNO));
                if (tunnel.PingInMs == -1)
                    info.Add("Unknown");
                else
                    info.Add(tunnel.PingInMs + " ms");
                info.Add(tunnel.Clients + " / " + tunnel.MaxClients);

                AddItem(info, true);

                int rating = GetTunnelRating(tunnel);
                if (rating < lowestTunnelRating)
                {
                    bestTunnelIndex = tunnelIndex;
                    lowestTunnelRating = rating;
                }

                tunnelIndex++;
            }

            if (tunnelHandler.Tunnels.Count > 0)
            {
                if (!isManuallySelectedTunnel)
                {
                    SelectedIndex = bestTunnelIndex;
                    isManuallySelectedTunnel = false;
                }
                else
                {
                    int manuallySelectedIndex = tunnelHandler.Tunnels.FindIndex(t => t.Address == manuallySelectedTunnelAddress);

                    if (manuallySelectedIndex == -1)
                    {
                        SelectedIndex = bestTunnelIndex;
                        isManuallySelectedTunnel = false;
                    }
                    else
                        SelectedIndex = manuallySelectedIndex;
                }
            }

            ListRefreshed?.Invoke(this, EventArgs.Empty);
        }

        private void TunnelHandler_TunnelPinged(int tunnelIndex)
        {
            XNAListBoxItem lbItem = GetItem(2, tunnelIndex);
            CnCNetTunnel tunnel = tunnelHandler.Tunnels[tunnelIndex];

            if (tunnel.PingInMs == -1)
                lbItem.Text = "Unknown";
            else
                lbItem.Text = tunnel.PingInMs + " ms";
                
            int rating = GetTunnelRating(tunnel);

            if (isManuallySelectedTunnel)
                return;

            if (rating < lowestTunnelRating)
            {
                bestTunnelIndex = tunnelIndex;
                lowestTunnelRating = rating;
                SelectedIndex = tunnelIndex;
            }
        }

        private int GetTunnelRating(CnCNetTunnel tunnel)
        {
            if (tunnel.Official || tunnel.Recommended)
            {
                int maxClients = tunnel.MaxClients > 400 ? 400 : tunnel.MaxClients;
                int clients = tunnel.Clients + 20 > maxClients ? maxClients : tunnel.Clients + 20;
            
                if (clients == maxClients)
                    return int.MaxValue;
            
                if (tunnel.PingInMs <= -1)
                {
                    int rating = int.MaxValue - 200000;
                    return rating + tunnel.Clients;
                }
            
                double usageRatio = (double)clients / maxClients;

                if (usageRatio == 0)
                    usageRatio = 0.1;

                usageRatio *= 100.0;

                int ping = tunnel.PingInMs <= 100 ? 100 : tunnel.PingInMs;
                
                return Convert.ToInt32(Math.Pow(ping, 2.0) * usageRatio);
            }
            else
            {
                int rating = int.MaxValue - 100000;
                return rating + tunnel.Clients;
            }
        }

        private void TunnelListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!IsValidIndexSelected())
                return;

            isManuallySelectedTunnel = true;
            manuallySelectedTunnelAddress = tunnelHandler.Tunnels[SelectedIndex].Address;
        }
    }
}
