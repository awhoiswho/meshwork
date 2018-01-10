//
// UserInfoDialog.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using FileFind.Meshwork.GtkClient.Menus;
using Glade;
using Gtk;

namespace FileFind.Meshwork.GtkClient.Windows
{
	public class UserInfoDialog : GladeDialog
	{
		Network network;
		Node    node;

		[Widget] Image avatarImage;

		[Widget] Label nickNameLabel;
		[Widget] Label realNameLabel;
		[Widget] Label emailLabel;

		[Widget] TreeView addressesTreeView;

		[Widget] Label clientNameLabel;
		[Widget] Label clientVersionLabel;
		[Widget] Label operatingSystemLabel;

		ListStore addressListStore;
			
		public UserInfoDialog (Window parent, Network network, Node node) : base (parent, "UserInfoDialog")
		{
			this.node    = node;
			this.network = network;
			
			base.Window.Title = node.ToString();

			avatarImage.Pixbuf = Gui.AvatarManager.GetAvatar(node);

			nickNameLabel.Markup = string.Format("<span weight=\"bold\" size=\"x-large\">{0}</span> on <i>{1}</i>\n<span font=\"monospace\" size=\"small\">{2}</span>",
			                                     node.NickName, network.NetworkName, Common.FormatFingerprint(node.NodeID, 4));
			
			realNameLabel.Text = node.RealName;
			emailLabel.Text    = node.Email;

			TreeViewColumn column;
			
			column = addressesTreeView.AppendColumn("Protocol", new CellRendererText(), "text", 0);

			column = addressesTreeView.AppendColumn("Address Details", new CellRendererText(), "text", 1);
			column.Expand = true;

			addressesTreeView.AppendColumn("Supported", new CellRendererText(), "text", 2);
			addressesTreeView.AppendColumn("Open Externally", new CellRendererText(), "text", 3);
			addressesTreeView.AppendColumn("Connectable", new CellRendererText(), "text", 4);

			addressListStore = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
		
			IDestination[] destinations = null;
			DestinationInfo[] destinationInfos;
			if (node.IsMe) {
				destinationInfos = Core.DestinationManager.DestinationInfos;
			} else {
				destinationInfos = node.GetTrustedNode().DestinationInfos.ToArray();
				destinations = node.GetTrustedNode().Destinations;
			}

			if (destinations != null) { 
				foreach (IDestination destination in destinations) {
					addressListStore.AppendValues(destination.FriendlyTypeName, destination.ToString(), "True", destination.IsOpenExternally.ToString(), destination.CanConnect.ToString());
				}
			}

			foreach (DestinationInfo info in destinationInfos) {
				if ((!info.Supported) || destinations == null) {
					if (node.IsMe) {
						addressListStore.AppendValues(info.FriendlyName, string.Join(", ", info.Data), info.Supported.ToString(), info.IsOpenExternally.ToString(), string.Empty);
					} else {
						addressListStore.AppendValues(info.FriendlyName, string.Join(", ", info.Data), "False", info.IsOpenExternally.ToString(), "False");
					}
				}
			}

			addressesTreeView.Model = addressListStore;

			clientNameLabel.Text      = node.ClientName;
			clientVersionLabel.Text   = node.ClientVersion;
			operatingSystemLabel.Text = node.OperatingSystem;
		}
		
		void HandleActionsButtonClicked (object sender, EventArgs args)
		{
			var menu = new UserMenu(network, node);
			menu.Popup((Widget)sender);
		}
	}
}
