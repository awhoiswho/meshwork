//
// IPDestination.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Net.Sockets;
using Meshwork.Common;
using Meshwork.Platform;

namespace Meshwork.Backend.Core.Destination
{
	public abstract class IPv6Destination : IPDestination
	{
		protected int prefixLength;

		public IPv6Destination (int prefixLength, IPAddress ip, uint port, bool isOpenExternally) : base (ip, port, isOpenExternally)
		{
			if (ip.AddressFamily != AddressFamily.InterNetworkV6) {
				throw new ArgumentException("ip must be IPv6");
			}

			this.prefixLength = prefixLength;
		}

		protected IPv6Destination () 
		{

		}

		public string NetworkPrefix {
			get {
				return IPv6Util.GetNetworkPrefix(prefixLength, ip);
			}
		}

		public int PrefixLength {
			get {
				return prefixLength;
			}
		}

		public override bool CanConnect {
			get {
				if (!Common.Common.SupportsIPv6) {
					return false;
				}
				
				if (IsExternal) {
					return base.IsOpenExternally && Core.HasExternalIPv6;
				} else {
					// Can't connect to link-local addresses if no interface is set.
					if (Core.Settings.IPv6LinkLocalInterfaceIndex == -1) {
						return false;
					}
				
					foreach (IDestination destination in Core.DestinationManager.Destinations) {

						// If this is an IPv6 address, we can connect only if
						// one of our Destinations is also IPv6 and has the
						// same network prefix (excluding link-local).
						if (destination is IPv6Destination && destination.IsExternal) {
							//IPv6Destination ipv6Destination = (IPv6Destination)destination;
							if (this.NetworkPrefix == ((IPv6Destination)destination).NetworkPrefix) {
								return true;
							}
		
						// In many (most?) cases, two nodes on the same LAN will
						// have only link-local IPv6 addresses. If we have a
						// matching external IPv4 address (i.e., we are behind
						// the same IPv4 NAT router), then we can connect.
						} else if (destination is IPv4Destination && destination.IsExternal) {
							IPAddress myAddress = ((IPv4Destination)destination).IPAddress;
							foreach (IDestination d in parentList) {
								if (d.IsExternal && d is IPv4Destination && myAddress.Equals(((IPv4Destination)d).IPAddress)) {
									return true;
								}
							}
						}
					}
					return false;
				}
			}
		}

		public override DestinationInfo CreateDestinationInfo ()
		{
			DestinationInfo info = new DestinationInfo();
			info.IsOpenExternally = this.IsOpenExternally;
			info.TypeName = this.GetType().ToString();
			info.Data = new string[] {
				ip.ToString(),
				prefixLength.ToString(),
				port.ToString()
			};
			return info;
		}
		
		public override int CompareTo (IDestination other)
		{
			if (other is IPv6Destination) {
				// Prefer internal IP addresses
				if (this.IsExternal && !other.IsExternal) {
					return 1;
				} else if (!this.IsExternal && other.IsExternal) {
					return -1;
				}
			} else if (other is IPv4Destination) {
				// Prefer IPv6 to IPv4
				return 1;
			}
			return 0;
		}
	}

	public abstract class IPv4Destination : IPDestination
	{
		public IPv4Destination (IPAddress ip, uint port, bool isOpenExternally) : base (ip, port, isOpenExternally)
		{
			if (ip.AddressFamily != AddressFamily.InterNetwork) {
				throw new ArgumentException(string.Format("ip must be IPv4 (was {0})", ip));
			}
		}

		protected IPv4Destination () 
		{

		}

		public override bool CanConnect {
			get {
				if (IsExternal) {
					return base.IsOpenExternally;
				} else {

					// Make sure we don't also have this (local) address.
					foreach (IDestination destination in Core.DestinationManager.Destinations) {
						if (destination is IPv4Destination && !destination.IsExternal) {
							IPAddress myAddress = ((IPv4Destination)destination).IPAddress;
							if (myAddress.Equals(base.IPAddress)) {
								return false;
							}
						}
					}
					
					// Only connect to local IPs that fall under a matching subnet.
					bool foundMatchingSubnet = false;
					foreach (IDestination destination in Core.DestinationManager.Destinations) {
						if (destination is IPv4Destination && !destination.IsExternal) {
							IPAddress myAddress = ((IPv4Destination)destination).IPAddress;
							var subnet = FindInterfaceWithIP(myAddress).SubnetMask;
							if (myAddress.IsInSameSubnet(base.IPAddress, subnet)) {
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
					
					foreach (IDestination destination in Core.DestinationManager.Destinations) {
						if (destination is IPv4Destination && destination.IsExternal) {
							IPAddress myAddress = ((IPv4Destination)destination).IPAddress;
							foreach (IDestination d in parentList) {
								if (d.IsExternal && d is IPv4Destination && myAddress.Equals(((IPv4Destination)d).IPAddress)) {
									return true;
								}
							}
						}
					}

					return false;
				}
			}
		}
	
		private InterfaceAddress FindInterfaceWithIP (IPAddress ip)
		{
			InterfaceAddress[] addresses = Core.OS.GetInterfaceAddresses();
			foreach (InterfaceAddress address in addresses) {
				if (address.Address.Equals(ip))
					return address;
			}
			throw new Exception("No interface found with address " + ip.ToString());
		}
		
		public override DestinationInfo CreateDestinationInfo()
		{
			DestinationInfo info = new DestinationInfo();
			info.IsOpenExternally = this.IsOpenExternally;
			info.TypeName = this.GetType().ToString();
			info.Data = new string[] { ip.ToString(), port.ToString() };
			return info;
		}
		
		public override int CompareTo (IDestination other)
		{
			if (other is IPv4Destination) {
				// Prefer internal IP addresses
				if (this.IsExternal && !other.IsExternal) {
					return 1;
				} else if (!this.IsExternal && other.IsExternal) {
					return -1;
				}
			} else if (other is IPv6Destination) {
				// Prefer IPv6 to IPv4
				return -1;
			}
			return 0;
		}
	}

	public abstract class IPDestination : DestinationBase
	{
		protected IPAddress ip;
		protected uint      port;

		public IPDestination (IPAddress ip, uint port, bool isOpenExternally)
		{
			this.ip = ip;
			this.port = port;

			base.isOpenExternally = isOpenExternally;
		}

		protected IPDestination ()
		{

		}

		public override bool IsExternal {
			get {
				return !Common.Common.IsInternalIP(this.IPAddress);
			}
		}

		public IPAddress IPAddress {
			get {
				return ip;
			}
		}

		public uint Port {
			get {
				return port;
			}
		}
	}
}
