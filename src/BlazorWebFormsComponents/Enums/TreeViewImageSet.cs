using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace BlazorWebFormsComponents.Enums
{

	public abstract class TreeViewImageSet
	{

		public static ArrowsTreeViewImageSet Arrows { get; } = new ArrowsTreeViewImageSet();
		public static BulletedListTreeViewImageSet BulletedList { get; } = new BulletedListTreeViewImageSet();
		public static BulletedList2TreeViewImageSet BulletedList2 { get; } = new BulletedList2TreeViewImageSet();
		public static BulletedList3TreeViewImageSet BulletedList3 { get; } = new BulletedList3TreeViewImageSet();
		public static BulletedList4TreeViewImageSet BulletedList4 { get; } = new BulletedList4TreeViewImageSet();
		public static ContactsTreeViewImageSet Contacts { get; } = new ContactsTreeViewImageSet();
		public static DefaultTreeViewImageSet Default { get; } = new DefaultTreeViewImageSet();
		public static EventsTreeViewImageSet Events { get; } = new EventsTreeViewImageSet();
		public static FAQTreeViewImageSet FAQ { get; } = new FAQTreeViewImageSet();
		public static InboxTreeViewImageSet Inbox { get; } = new InboxTreeViewImageSet();
		public static MSDNTreeViewImageSet MSDN { get; } = new MSDNTreeViewImageSet();
		public static NewsTreeViewImageSet News { get; } = new NewsTreeViewImageSet();
		public static SimpleTreeViewImageSet Simple { get; } = new SimpleTreeViewImageSet();
		public static Simple2TreeViewImageSet Simple2 { get; } = new Simple2TreeViewImageSet();
		public static WindowsTreeViewImageSet Windows { get; } = new WindowsTreeViewImageSet();
		public static XP_ExplorerTreeViewImageSet XP_Explorer { get; } = new XP_ExplorerTreeViewImageSet();

		public virtual bool HasCollapse => true;
		public virtual bool HasNodes => true;

		public virtual string Collapse => FormatFilename("Collapse");
		public virtual string Expand => FormatFilename("Expand");
		public virtual string NoExpand  => FormatFilename("NoExpand");
		public virtual string LeafNode => FormatFilename("LeafNode");
		public virtual string RootNode => FormatFilename("RootNode");
		public virtual string ParentNode => FormatFilename("ParentNode");

		public string FormatFilename(string imageFilebase) =>
			string.Concat(this.GetType().Name.Replace("TreeViewImageSet", ""), "_", imageFilebase, ".gif");

	}

	public class ArrowsTreeViewImageSet : TreeViewImageSet {

		public override bool HasNodes => false;

	}
	public class BulletedListTreeViewImageSet : TreeViewImageSet {

		public override bool HasCollapse => false;

	}
	public class BulletedList2TreeViewImageSet : TreeViewImageSet {
		public override bool HasCollapse => false;

	}
	public class BulletedList3TreeViewImageSet : TreeViewImageSet {
		public override bool HasCollapse => false;

	}

	public class BulletedList4TreeViewImageSet : TreeViewImageSet {

		public override bool HasCollapse => false;

	}
	public class ContactsTreeViewImageSet : TreeViewImageSet {

		public override bool HasNodes => false;

	}
	public class DefaultTreeViewImageSet : TreeViewImageSet {

		public override bool HasCollapse => false;

	}
	public class EventsTreeViewImageSet : TreeViewImageSet {

		public override bool HasCollapse => false;

	}
	public class FAQTreeViewImageSet : TreeViewImageSet {

		public override bool HasCollapse => false;

	}
	public class InboxTreeViewImageSet : TreeViewImageSet {

		public override bool HasCollapse => false;

	}
	public class MSDNTreeViewImageSet : TreeViewImageSet {

		public override bool HasNodes => false;

	}
	public class NewsTreeViewImageSet : TreeViewImageSet {

		public override bool HasCollapse => false;

	}
	public class SimpleTreeViewImageSet : TreeViewImageSet {

		public override bool HasCollapse => false;

		public override bool HasNodes => false;

	}
	public class Simple2TreeViewImageSet : TreeViewImageSet {

		public override bool HasCollapse => false;

		public override bool HasNodes => false;

	}
	public class WindowsTreeViewImageSet : TreeViewImageSet {

		public override bool HasNodes => false;

	}
	public class XP_ExplorerTreeViewImageSet : TreeViewImageSet { }

}
