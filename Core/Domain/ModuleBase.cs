using System;

using NHibernate;

namespace Cuyahoga.Core.Domain
{
	/// <summary>
	/// The base class for all Cuyahoga modules
	/// </summary>
	public abstract class ModuleBase
	{
		private Section _section;
		private ISession _session;

		/// <summary>
		/// The NHibernate session from the current ASP.NET context.
		/// </summary>
		protected ISession NHSession
		{
			get 
			{ 
				if (this._session == null)
				{
					// There is no NHibernate session. Raise an event to obtain the session
					// stored in the current ASP.NET context.
					NHSessionEventArgs args = new NHSessionEventArgs();
					OnNHSessionRequired(args);
					this._session = args.Session;
				}
				return this._session;
			}
		}

		public delegate void NHSessionEventHandler(object sender, NHSessionEventArgs e);

		public event NHSessionEventHandler NHSessionRequired;

		protected void OnNHSessionRequired(NHSessionEventArgs e)
		{
			if (NHSessionRequired != null)
			{
				NHSessionRequired(this, e);
			}
		}

		public event EventHandler SessionFactoryRebuilt;

		protected void OnSessionFactoryRebuilt(EventArgs e)
		{
			if (SessionFactoryRebuilt != null)
			{
				SessionFactoryRebuilt(this, e);
			}
		}

		/// <summary>
		/// Property Section (Section)
		/// </summary>
		public Section Section
		{
			get { return this._section; }
			set { this._section = value; }
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public ModuleBase()
		{
		}

		public class NHSessionEventArgs : EventArgs
		{
			private ISession _session;

			/// <summary>
			/// Property Session (ISession)
			/// </summary>
			public ISession Session
			{
				get { return this._session; }
				set { this._session = value; }
			}

			/// <summary>
			/// Default constructor.
			/// </summary>
			public NHSessionEventArgs()
			{
			}
		}
	}
}
