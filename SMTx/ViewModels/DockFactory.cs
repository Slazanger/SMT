using System;
using System.Collections.Generic;
using SMTx.ViewModels.Docks;
using SMTx.ViewModels.Documents;
using SMTx.ViewModels.Tools;
using SMTx.ViewModels.Views;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace SMTx.ViewModels;

public class DockFactory : Factory
{
    private readonly object _context;
    private IRootDock? _rootDock;
    private IDocumentDock? _documentDock;

    public DockFactory(object context)
    {
        _context = context;
    }

    public override IDocumentDock CreateDocumentDock() => new CustomDocumentDock();

    public override IRootDock CreateLayout()
    {
        var document1 = new RegionViewModel { Id = "RegionID", Title = "Region", CanClose = false };
        var document2 = new UniverseViewModel { Id = "UniverseID", Title = "Universe", CanClose = false };
        var document3 = new SimpleRegionsViewModel { Id = "SimpleRegionsID", Title = "Regions", CanClose = false };

        var anomsTool = new AnomsViewModel { Id = "AnomsID", Title = "Anoms", CanClose = false };
        var charactersTool = new CharactersViewModel { Id = "CharactersID", Title = "Characters", CanClose = false };
        var fleetTool = new FleetViewModel { Id = "FleetID", Title="Fleet", CanClose=false };
        var gameLogFeedTool = new GameLogFeedViewModel { Id = "GameLogFeedID", Title = "Game Log", CanClose = false };
        var intelFeedTool = new IntelFeedViewModel { Id = "IntelFeedID", Title = "Intel", CanClose = false };
        var jumpGateNetworkTool = new JumpGateNetworkViewModel { Id = "JumpGateNetworkID", Title = "Ansiblex Gates", CanClose = false };
        var jumpRoutePlannerTool = new JumpRoutePlannerViewModel { Id = "JumpRoutePlannerID", Title = "Jump Planner", CanClose = false };
        var routeTool = new RouteViewModel { Id = "RouteID", Title = "Route", CanClose = false };
        var sovCampaignsTool = new SOVCampaignsViewModel { Id = "SovCampaignsID", Title = "SOV Campaigns", CanClose = false };
        var stormsTool = new StormsViewModel { Id = "StormsID", Title = "Storms", CanClose = false };
        var theraTool = new TheraViewModel { Id = "TheraID", Title = "Thera", CanClose = false };
        var zkillFeedTool = new ZKillFeedViewModel { Id = "ZKillFeedID", Title = "ZKill Feed", CanClose = false };


        var leftDock = new ProportionalDock
        {
            Proportion = 0.25,
            Orientation = Orientation.Vertical,
            ActiveDockable = null,
            VisibleDockables = CreateList<IDockable>
            (
                new ToolDock
                {
                    ActiveDockable = zkillFeedTool,
                    VisibleDockables = CreateList<IDockable>(zkillFeedTool),
                    Alignment = Alignment.Left
                },
                new ProportionalDockSplitter(),
                new ToolDock
                {
                    ActiveDockable = intelFeedTool,
                    VisibleDockables = CreateList<IDockable>(intelFeedTool, gameLogFeedTool, fleetTool),
                    Alignment = Alignment.Bottom
                }
            )
        };

        var rightDock = new ProportionalDock
        {
            Proportion = 0.25,
            Orientation = Orientation.Vertical,
            ActiveDockable = null,
            VisibleDockables = CreateList<IDockable>
            (
                new ToolDock
                {
                    ActiveDockable = theraTool,
                    VisibleDockables = CreateList<IDockable>(theraTool, anomsTool, charactersTool, sovCampaignsTool),
                    Alignment = Alignment.Top,
                    GripMode = GripMode.Hidden
                },
                new ProportionalDockSplitter(),
                new ToolDock
                {
                    ActiveDockable = routeTool,
                    VisibleDockables = CreateList<IDockable>(routeTool, jumpGateNetworkTool, jumpRoutePlannerTool),
                    Alignment = Alignment.Right,
                    GripMode = GripMode.AutoHide
                }
            )
        };

        var documentDock = new CustomDocumentDock
        {
            IsCollapsable = false,
            ActiveDockable = document1,
            VisibleDockables = CreateList<IDockable>(document1, document2, document3),
            CanCreateDocument = true
        };

        var mainLayout = new ProportionalDock
        {
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>
            (
                leftDock,
                new ProportionalDockSplitter(),
                documentDock,
                new ProportionalDockSplitter(),
                rightDock
            )
        };



        var homeView = new HomeViewModel
        {
            Id = "Home",
            Title = "Home",
            ActiveDockable = mainLayout,
            VisibleDockables = CreateList<IDockable>(mainLayout)
        };

        var rootDock = CreateRootDock();

        rootDock.IsCollapsable = false;
        rootDock.ActiveDockable = mainLayout;
        rootDock.DefaultDockable = mainLayout;
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

        _documentDock = documentDock;
        _rootDock = rootDock;
            
        return rootDock;
    }

    public override void InitLayout(IDockable layout)
    {
        ContextLocator = new Dictionary<string, Func<object>>
        {
            ["RegionID"] = () => new RegionViewModel(),
            ["UniverseID"] = () => new UniverseViewModel(),
            ["SimpleRegionsID"] = () => new SimpleRegionsViewModel(),


            ["AnomsID"] = () => new AnomsViewModel(),
            ["CharactersID"] = () => new CharactersViewModel(),
            ["FleetID"] = () => new FleetViewModel(),
            ["GameLogFeedID"] = () => new GameLogFeedViewModel(),
            ["IntelFeedID"] = () => new IntelFeedViewModel(),
            ["JumpGateNetworkID"] = () => new JumpGateNetworkViewModel(),
            ["JumpRoutePlannerID"] = () => new JumpRoutePlannerViewModel(),
            ["RouteID"] = () => new RouteViewModel(),
            ["SovCampaignsID"] = () => new SOVCampaignsViewModel(),
            ["StormsID"] = () => new StormsViewModel(),
            ["TheraID"] = () => new TheraViewModel(),
            ["ZKillFeedID"] = () => new ZKillFeedViewModel(),


            ["Dashboard"] = () => layout,
            ["Home"] = () => _context
        };

        DockableLocator = new Dictionary<string, Func<IDockable?>>()
        {
            ["Root"] = () => _rootDock,
            ["Documents"] = () => _documentDock
        };

        HostWindowLocator = new Dictionary<string, Func<IHostWindow>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };

        base.InitLayout(layout);
    }
}
