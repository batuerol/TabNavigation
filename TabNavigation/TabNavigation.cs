//------------------------------------------------------------------------------
// <copyright file="TabNavigation.cs" Author="Batuhan Erol">
//     Copyright (c) Batuhan Erol.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Collections.Generic;

namespace TabGroupSwitch
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TabNavigation
    {
        /// <summary>
        /// Command ID.
        /// </summary>        
        public const int CommandPingPong = 0x0100;
        public const int CommandJumpLeft = 0x0101;
        public const int CommandJumpRight = 0x0102;
        public const int CommandJumpUp = 0x0103;
        public const int CommandJumpDown = 0x0104;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("629661e4-603b-446f-86b1-9defbda7d529");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabNavigation"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private TabNavigation(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                commandService.AddCommand(new MenuCommand(this.PingPongCallback, new CommandID(CommandSet, CommandPingPong)));

                commandService.AddCommand(new MenuCommand(this.JumpCallback, new CommandID(CommandSet, CommandJumpUp)));
                commandService.AddCommand(new MenuCommand(this.JumpCallback, new CommandID(CommandSet, CommandJumpDown)));
                commandService.AddCommand(new MenuCommand(this.JumpCallback, new CommandID(CommandSet, CommandJumpLeft)));
                commandService.AddCommand(new MenuCommand(this.JumpCallback, new CommandID(CommandSet, CommandJumpRight)));
            }

            DTE dte = (DTE)(this.ServiceProvider.GetService(typeof(DTE)));

            WindowEvents _windowEvents = dte.Events.WindowEvents;
            _windowEvents.WindowActivated += WindowEvents_WindowActivated;
            _windowEvents.WindowClosing += WindowEvents_WindowClosing;
            _windowEvents.WindowCreated += WindowEvents_WindowCreated;
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TabNavigation Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new TabNavigation(package);
        }

        private void WindowEvents_WindowCreated(Window Window)
        {
            //throw new NotImplementedException();
        }

        private void WindowEvents_WindowActivated(Window GotFocus, Window LostFocus)
        {
            if (GotFocus.Kind == "Document")
            {
                if (LostFocus != null && LostFocus.Kind == "Document")
                {
                    mLastActiveDocument = LostFocus;
                }
                //if (mLastActiveDocument == GotFocus)
                //{
                //    if (LostFocus != null && LostFocus.Kind == "Document")
                //    {
                //        mLastActiveDocument = LostFocus;
                //    }
                //}
            }
            else
            {
                mLastActiveDocument = null;
            }
        }

        private void WindowEvents_WindowClosing(Window Window)
        {
            if (Window == mLastActiveDocument)
            {
                mLastActiveDocument = null;
            }
        }

        private Window mLastActiveDocument = null;

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>        
        private void JumpCallback(object sender, EventArgs e)
        {
            DTE dte = (DTE)(this.ServiceProvider.GetService(typeof(DTE)));
            int commandId = ((MenuCommand)sender).CommandID.ID;
            bool vertical = commandId == CommandJumpLeft || commandId == CommandJumpRight;

            List<Window> activeDocuments = GetActiveDocumentWindows();
            // Get focused window.
            Window activeWindow = dte.ActiveDocument.ActiveWindow;

            if (vertical)
            {
                activeDocuments.Sort((x, y) => x.Left < y.Left ? -1 : 1);
            }
            else
            {
                activeDocuments.Sort((x, y) => x.Top < y.Top ? -1 : 1);
            }

            int index = activeDocuments.FindIndex(w => w == activeWindow);
            if (vertical)
            {
                if (commandId == CommandJumpLeft)
                    --index;
                else
                    ++index;
            }
            else
            {
                if (commandId == CommandJumpUp)
                    --index;
                else
                    ++index;
            }

            if (index < 0)
                index += activeDocuments.Count;

            index %= activeDocuments.Count;            
            activeDocuments[index].Activate();               
        }

        private void PingPongCallback(object sender, EventArgs e)
        {
            DTE dte = (DTE)(this.ServiceProvider.GetService(typeof(DTE)));
            List<Window> activeDocuments = GetActiveDocumentWindows();

            if (activeDocuments.Count < 1)
                return;

            Window temp = dte.ActiveDocument.ActiveWindow;
            // TODO(batuhan): Do I actually want to do this?            
            if (mLastActiveDocument == null)
            {
                mLastActiveDocument = temp;
                Window w = activeDocuments.Find(x => x != mLastActiveDocument);
                if (w != null)
                    w.Activate();
            }
            else
            {
                mLastActiveDocument.Activate();
                //mLastActiveDocument = temp;
            }
        }

        private List<Window> GetActiveDocumentWindows()
        {
            DTE dte = (DTE)(ServiceProvider.GetService(typeof(DTE)));
            List<Window> result = new List<Window>();

            foreach (Window w in dte.Windows)
            {
                if (w.Kind == "Document" && (w.Left > 0 || w.Top > 0))
                {
                    result.Add(w);
                }
            }

            return result;
        }
    }
}
