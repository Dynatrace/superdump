using System.Collections.Generic;

namespace SuperDump.Models {
	public interface ITaggableItem {
		ISet<SDTag> Tags { get; }
	}
}
