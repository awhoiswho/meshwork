﻿using System;
using System.Net;
using System.Net.Sockets;
using Meshwork.Common;
using Meshwork.Platform;

namespace Meshwork.Backend.Core.Destination
{
    public abstract class IPv4Destination : IPDestination
    {
        protected IPv4Destination (Core core, IPAddress ip, uint port, bool isOpenExternally)
            : base (core, ip, port, isOpenExternally)
        {
            if (ip.AddressFamily != AddressFamily.InterNetwork) {
                throw new ArgumentException($"ip must be IPv4 (was {ip})");
            }
        }

        public override bool CanConnect {
            get {
                if (IsExternal) {
                    return IsOpenExternally;
                }
                // Make sure we don't also have this (local) address.
                foreach (var destination in Core.DestinationManager.Destinations) {
                    if (destination is IPv4Destination && !destination.IsExternal) {
                        var myAddress = ((IPv4Destination)destination).IPAddress;
                        if (myAddress.Equals(IPAddress)) {
                            return false;
                        }
                    }
                }
					
                // Only connect to local IPs that fall under a matching subnet.
                var foundMatchingSubnet = false;
                foreach (var destination in Core.DestinationManager.Destinations) {
                    if (destination is IPv4Destination && !destination.IsExternal) {
                        var myAddress = ((IPv4Destination)destination).IPAddress;
                        var subnet = FindInterfaceWithIP(myAddress).SubnetMask;
                        if (myAddress.IsInSameSubnet(IPAddress, subnet)) {
                            foundMatchingSubnet = true;
                            break;
                        }
                    }
                }
					
                if (!foundMatchingSubnet)
                    return false;
					
                // If this is an IPv4 address, we can connect only
                // if one of our external Destinations matches another one
                // of their (external) Destinations. This means that we both
                // have the same external IP address (both are behind the
                // same NAT router). Under certain situations, a NAT'd
                // network may have multiple public IP Addresses. We do not
                // currently support this case.
                // Multiple interfaces with private addresses are not currently well supported either.
					
                foreach (var destination in Core.DestinationManager.Destinations) {
                    if (destination is IPv4Destination && destination.IsExternal) {
                        var myAddress = ((IPv4Destination)destination).IPAddress;
                        foreach (var d in ParentList) {
                            if (d.IsExternal && d is IPv4Destination && myAddress.Equals(((IPv4Destination)d).IPAddress)) {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }
	
        private InterfaceAddress FindInterfaceWithIP (IPAddress ip)
        {
            var addresses = Core.Platform.GetInterfaceAddresses();
            foreach (var address in addresses) {
                if (address.Address.Equals(ip))
                    return address;
            }
            throw new Exception("No interface found with address " + ip);
        }
		
        public override DestinationInfo CreateDestinationInfo()
        {
            return new DestinationInfo
            {
                IsOpenExternally = IsOpenExternally,
                TypeName = GetType().ToString(),
                Data = new[] {IPAddress.ToString(), Port.ToString()}
            };
        }
		
        public override int CompareTo (IDestination other)
        {
            if (other is IPv4Destination)
            {
                // Prefer internal IP addresses
                if (IsExternal && !other.IsExternal) {
                    return 1;
                }
                if (!IsExternal && other.IsExternal) {
                    return -1;
                }
            } else if (other is IPv6Destination) {
                // Prefer IPv6 to IPv4
                return -1;
            }
            return 0;
        }
    }
}