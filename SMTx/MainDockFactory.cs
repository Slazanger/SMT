using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Data;
using SMTx.Models;
using SMTx.ViewModels;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.ReactiveUI;
using Dock.Model.ReactiveUI.Controls;
using ReactiveUI;


namespace SMTx
{
    public class MainDockFactory : Factory
    {
        private IDocumentDock _documentDock;
        private readonly object _context;

        public MainDockFactory(object context)
        {
            _context = context;
        }

        public override IRootDock CreateLayout()
        {


            var document1 = new RegionViewModel
            {
                Id = "RegionID",
                Title = "Region"
            };

            var document2 = new UniverseViewModel
            {
                Id = "UniverseID",
                Title = "Universe"
            };

            var document3 = new SimpleRegionsViewModel
            {
                Id = "SimpleRegionsID",
                Title = "Regions"
            };


            var anomsTool = new AnomsViewModel
            {
                Id = "AnomsID",
                Title = "Anoms",
            };

            var charactersTool = new CharactersViewModel
            {
                Id = "CharactersID",
                Title = "Characters",
            };

            var gameLogFeedTool = new GameLogFeedViewModel
            {
                Id = "GameLogFeedID",
                Title = "Game Log",
            };

            var intelFeedTool = new IntelFeedViewModel
            {
                Id = "IntelFeedID",
                Title = "Intel",
            };

            var jumpGateNetworkTool = new JumpGateNetworkViewModel
            {
                Id = "JumpGateNetworkID",
                Title = "Ansiblex Gates",
            };

            var jumpRoutePlannerTool = new JumpRoutePlannerViewModel
            {
                Id = "JumpRoutePlannerID",
                Title = "Jump Planner",
            };

            var routeTool = new RouteViewModel
            {
                Id = "RouteID",
                Title = "Route",
            };

            var sovCampaignsTool = new SOVCampaignsViewModel
            {
                Id = "SovCampaignsID",
                Title = "SOV Campaigns",
            };

            var stormsTool = new StormsViewModel
            {
                Id = "StormsID",
                Title = "Storms",
            };

            var theraTool = new TheraViewModel
            {
                Id = "TheraID",
                Title = "Thera",
            };

            var zkillFeedTool = new ZKillFeedViewModel
            {
                Id = "ZKillFeedID",
                Title = "ZKill Feed",
            };

            var documentDock = new DocumentDock
            {
                Id = "DocumentsPane",
                Title = "DocumentsPane",
                Proportion = 0.7,
                ActiveDockable = document1,
                VisibleDockables = CreateList<IDockable>
                (
                    document1,
                    document2,
                    document3
                )
            };

            documentDock.CanCreateDocument = false;

            var mainLayout = new ProportionalDock
            {
                Id = "MainLayout",
                Title = "MainLayout",
                Proportion = double.NaN,
                Orientation = Orientation.Horizontal,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>
                (
                   new ProportionalDock
                   {
                       Id = "LeftPane",
                       Title = "LeftPane",
                       Proportion = 0.15,
                       Orientation = Orientation.Vertical,
                       ActiveDockable = null,
                       VisibleDockables = CreateList<IDockable>
                       (
                           new ToolDock
                           {
                               Id = "LeftPaneTop",
                               Title = "LeftPaneTop",
                               Proportion = double.NaN,
                               ActiveDockable = zkillFeedTool,
                               VisibleDockables = CreateList<IDockable>
                               (
                                   zkillFeedTool
                              ),
                               Alignment = Alignment.Left,
                               GripMode = GripMode.Visible
                           },
                           new ProportionalDockSplitter()
                           {
                               Id = "LeftPaneTopSplitter",
                               Title = "LeftPaneTopSplitter"
                           },
                           new ToolDock
                           {
                               Id = "LeftPaneBottom",
                               Title = "LeftPaneBottom",
                               Proportion = double.NaN,
                               ActiveDockable = intelFeedTool,
                               VisibleDockables = CreateList<IDockable>
                               (
                                   intelFeedTool,
                                   gameLogFeedTool
                               ),
                               Alignment = Alignment.Left,
                               GripMode = GripMode.Visible
                           }
                       )
                   },
                   new ProportionalDockSplitter()
                   {
                       Id = "LeftSplitter",
                       Title = "LeftSplitter"
                   },
                   documentDock,
                   new ProportionalDockSplitter()
                   {
                       Id = "RightSplitter",
                       Title = "RightSplitter"
                   },
                   new ProportionalDock
                   {
                       Id = "RightPane",
                       Title = "RightPane",
                       Proportion = 0.15,
                       Orientation = Orientation.Vertical,
                       ActiveDockable = null,
                       VisibleDockables = CreateList<IDockable>
                       (

                           new ToolDock
                           {
                               Id = "RightPaneTop",
                               Title = "RightPaneTop",
                               Proportion = double.NaN,
                               ActiveDockable = charactersTool,
                               AutoHide = true,
                               VisibleDockables = CreateList<IDockable>
                               (
                                   anomsTool,
                                   charactersTool,
                                   sovCampaignsTool,
                                   stormsTool
                               ),
                               Alignment = Alignment.Right,
                               GripMode = GripMode.Visible
                           },

                           new ToolDock
                           {
                               Id = "RightPaneBottom",
                               Title = "RightPaneBottom",
                               Proportion = double.NaN,
                               ActiveDockable = theraTool,
                               AutoHide = true,
                               VisibleDockables = CreateList<IDockable>
                               (
                                   jumpGateNetworkTool,
                                   jumpRoutePlannerTool,
                                   routeTool,
                                   theraTool
                               ),
                               Alignment = Alignment.Right,
                               GripMode = GripMode.Visible
                           }

                       )
                   }
               )
            };


            var mainView = new MainViewModel
            {
                Id = "Main",
                Title = "Main",
                ActiveDockable = mainLayout,
                VisibleDockables = CreateList<IDockable>(mainLayout)
            };

            var root = CreateRootDock();

            root.Id = "Root";
            root.Title = "Root";
            root.ActiveDockable = mainView;
            root.DefaultDockable = mainView;
            root.VisibleDockables = CreateList<IDockable>(mainView);

            _documentDock = documentDock;

            return root;
        }

        public override void InitLayout(IDockable layout)
        {
            this.ContextLocator = new Dictionary<string, Func<object>>
            {
                [nameof(IRootDock)] = () => _context,
                [nameof(IProportionalDock)] = () => _context,
                [nameof(IDocumentDock)] = () => _context,
                [nameof(IToolDock)] = () => _context,
                [nameof(IProportionalDockSplitter)] = () => _context,
                [nameof(IDockWindow)] = () => _context,
                [nameof(IDocument)] = () => _context,
                [nameof(ITool)] = () => _context,

                ["RegionID"] = () => new Region(),
                ["UniverseID"] = () => new Universe(),
                ["SimpleRegionsID"] = () => new SimpleRegions(),


                ["AnomsID"] = () => new Anoms(),
                ["CharactersID"] = () => new Characters(),
                ["GameLogFeedID"] = () => new GameLogFeed(),
                ["IntelFeedID"] = () => new IntelFeed(),
                ["JumpGateNetworkID"] = () => new JumpGateNetwork(),
                ["JumpRoutePlannerID"] = () => new JumpRoutePlanner(),
                ["RouteID"] = () => new Route(),
                ["SovCampaignsID"] = () => new SOVCampaigns(),
                ["StormsID"] = () => new Storms(),
                ["TheraID"] = () => new Thera(),
                ["ZKillFeedID"] = () => new ZkillFeed(),



                ["LeftPane"] = () => _context,
                ["LeftPaneTop"] = () => _context,
                ["LeftPaneTopSplitter"] = () => _context,
                ["LeftPaneBottom"] = () => _context,
                ["RightPane"] = () => _context,
                ["RightPaneTop"] = () => _context,
                ["RightPaneTopSplitter"] = () => _context,
                ["RightPaneBottom"] = () => _context,
                ["DocumentsPane"] = () => _context,
                ["MainLayout"] = () => _context,
                ["LeftSplitter"] = () => _context,
                ["RightSplitter"] = () => _context,
                ["MainLayout"] = () => _context,
                ["Main"] = () => _context,
            };

            this.HostWindowLocator = new Dictionary<string, Func<IHostWindow>>
            {
                [nameof(IDockWindow)] = () =>
                {
                    var hostWindow = new HostWindow()
                    {
                        [!HostWindow.TitleProperty] = new Binding("ActiveDockable.Title")
                    };
                    return hostWindow;
                }
            };

            this.DockableLocator = new Dictionary<string, Func<IDockable>>
            {
            };

            base.InitLayout(layout);

            this.SetActiveDockable(_documentDock);
            this.SetFocusedDockable(_documentDock, _documentDock.VisibleDockables?.FirstOrDefault());
        }

    }
}