using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Web;

namespace d360.model.helpers
{
	public class ObjectState<T>
	{
		public T Value { get; set; }
		public T PreviousValue { get; set; }
	}
	public class ChangeLogTracker<T> where T: class
	{
		private List<ObjectState<T>> objectStates = new List<ObjectState<T>>();
		public void Add(ObjectState<T> state)
		{
			objectStates.Add(state);
		}

	}
}