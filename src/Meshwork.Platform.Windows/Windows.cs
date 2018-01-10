//
// Windows.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Meshwork.Platform.Windows
{
	/// <summary>
	/// Description of Windows.
	/// </summary>
	public class WindowsPlatform : IPlatform
	{
		public InterfaceAddress[] GetInterfaceAddresses ()
		{
			List<InterfaceAddress> result = new List<InterfaceAddress> ();
			
			int index = 0;
			foreach (NetworkInterface iface in NetworkInterface.GetAllNetworkInterfaces()) {				
				foreach (UnicastIPAddressInformation ip in iface.GetIPProperties().UnicastAddresses) {
					if (ip.Address.AddressFamily == AddressFamily.InterNetwork && ip.IPv4Mask != null)
						result.Add(new InterfaceAddress(index, iface.Name, ip.Address, ip.IPv4Mask));
					// FIXME: How do I get the prefix length?
					//else if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
						//results.Add(new InterfaceAddress(iface.Id, iface.Name, ip.Address, prefixLength);
				}				
				index ++;
			}
			
			return result.ToArray();
		}

	    public void SetProcessName(string name)
	    {
	        /* do nothing */
	    }

	    public string OSName => "Windows";

	    public string UserName {
			get {
				return string.Empty;
			}
		}
		
		public string RealName {
			get {
				return string.Empty;
			}
		}

		public string VersionInfo {
			get {
				return "Windows"; // XXX: Get winver
			}
		}
	}
}
