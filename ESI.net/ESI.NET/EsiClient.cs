using ESI.NET.Logic;
using ESI.NET.Models.SSO;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ESI.NET
{
    public class EsiClient : IEsiClient
    {
        readonly HttpClient client;
        readonly EsiConfig config;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public EsiClient(IOptions<EsiConfig> _config)
        {
            config = _config.Value;
            client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

            // Enforce user agent value
            if (string.IsNullOrEmpty(config.UserAgent))
                throw new ArgumentException("For your protection, please provide an X-User-Agent value. This can be your character name and/or project name. CCP will be more likely to contact you rather than just cut off access to ESI if you provide something that can identify you within the New Eden galaxy.");
            else
                client.DefaultRequestHeaders.Add("X-User-Agent", config.UserAgent);

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

            SSO = new SsoLogic(client, config);
            Alliance = new AllianceLogic(client, config);
            Assets = new AssetsLogic(client, config);
            Bookmarks = new BookmarksLogic(client, config);
            Calendar = new CalendarLogic(client, config);
            Character = new CharacterLogic(client, config);
            Clones = new ClonesLogic(client, config);
            Contacts = new ContactsLogic(client, config);
            Contracts = new ContractsLogic(client, config);
            Corporation = new CorporationLogic(client, config);
            Dogma = new DogmaLogic(client, config);
            FactionWarfare = new FactionWarfareLogic(client, config);
            Fittings = new FittingsLogic(client, config);
            Fleets = new FleetsLogic(client, config);
            Incursions = new IncursionsLogic(client, config);
            Industry = new IndustryLogic(client, config);
            Insurance = new InsuranceLogic(client, config);
            Killmails = new KillmailsLogic(client, config);
            Location = new LocationLogic(client, config);
            Loyalty = new LoyaltyLogic(client, config);
            Mail = new MailLogic(client, config);
            Market = new MarketLogic(client, config);
            Opportunities = new OpportunitiesLogic(client, config);
            PlanetaryInteraction = new PlanetaryInteractionLogic(client, config);
            Routes = new RoutesLogic(client, config);
            Search = new SearchLogic(client, config);
            Skills = new SkillsLogic(client, config);
            Sovereignty = new SovereigntyLogic(client, config);
            Status = new StatusLogic(client, config);
            Universe = new UniverseLogic(client, config);
            UserInterface = new UserInterfaceLogic(client, config);
            Wallet = new WalletLogic(client, config);
            Wars = new WarsLogic(client, config);
        }

        public SsoLogic SSO { get; set; }
        public AllianceLogic Alliance { get; set; }
        public AssetsLogic Assets { get; set; }
        public BookmarksLogic Bookmarks { get; set; }
        public CalendarLogic Calendar { get; set; }
        public CharacterLogic Character { get; set; }
        public ClonesLogic Clones { get; set; }
        public ContactsLogic Contacts { get; set; }
        public ContractsLogic Contracts { get; set; }
        public CorporationLogic Corporation { get; set; }
        public DogmaLogic Dogma { get; set; }
        public FactionWarfareLogic FactionWarfare { get; set; }
        public FleetsLogic Fleets { get; set; }
        public FittingsLogic Fittings { get; set; }
        public IncursionsLogic Incursions { get; set; }
        public IndustryLogic Industry { get; set; }
        public InsuranceLogic Insurance { get; set; }
        public KillmailsLogic Killmails { get; set; }
        public LocationLogic Location { get; set; }
        public LoyaltyLogic Loyalty { get; set; }
        public MailLogic Mail { get; set; }
        public MarketLogic Market { get; set; }
        public OpportunitiesLogic Opportunities { get; set; }
        public PlanetaryInteractionLogic PlanetaryInteraction { get; set; }
        public RoutesLogic Routes { get; set; }
        public SearchLogic Search { get; set; }
        public SkillsLogic Skills { get; set; }
        public StatusLogic Status { get; set; }
        public SovereigntyLogic Sovereignty { get; set; }
        public UniverseLogic Universe { get; set; }
        public UserInterfaceLogic UserInterface { get; set; }
        public WalletLogic Wallet { get; set; }
        public WarsLogic Wars { get; set; }


        public void SetCharacterData(AuthorizedCharacterData data)
        {
            Assets = new AssetsLogic(client, config, data);
            Bookmarks = new BookmarksLogic(client, config, data);
            Calendar = new CalendarLogic(client, config, data);
            Character = new CharacterLogic(client, config, data);
            Clones = new ClonesLogic(client, config, data);
            Contacts = new ContactsLogic(client, config, data);
            Contracts = new ContractsLogic(client, config, data);
            Corporation = new CorporationLogic(client, config, data);
            FactionWarfare = new FactionWarfareLogic(client, config, data);
            Fittings = new FittingsLogic(client, config, data);
            Fleets = new FleetsLogic(client, config, data);
            Industry = new IndustryLogic(client, config, data);
            Killmails = new KillmailsLogic(client, config, data);
            Location = new LocationLogic(client, config, data);
            Loyalty = new LoyaltyLogic(client, config, data);
            Mail = new MailLogic(client, config, data);
            Market = new MarketLogic(client, config, data);
            Opportunities = new OpportunitiesLogic(client, config, data);
            PlanetaryInteraction = new PlanetaryInteractionLogic(client, config, data);
            Search = new SearchLogic(client, config, data);
            Skills = new SkillsLogic(client, config, data);
            UserInterface = new UserInterfaceLogic(client, config, data);
            Wallet = new WalletLogic(client, config, data);
            Universe = new UniverseLogic(client, config, data);
        }

        public void SetIfNoneMatchHeader(string eTag)
            => EsiRequest.ETag = eTag;
    }

    public interface IEsiClient
    {
        SsoLogic SSO { get; set; }
        AllianceLogic Alliance { get; set; }
        AssetsLogic Assets { get; set; }
        BookmarksLogic Bookmarks { get; set; }
        CalendarLogic Calendar { get; set; }
        CharacterLogic Character { get; set; }
        ClonesLogic Clones { get; set; }
        ContactsLogic Contacts { get; set; }
        ContractsLogic Contracts { get; set; }
        CorporationLogic Corporation { get; set; }
        DogmaLogic Dogma { get; set; }
        FactionWarfareLogic FactionWarfare { get; set; }
        FittingsLogic Fittings { get; set; }
        FleetsLogic Fleets { get; set; }
        IncursionsLogic Incursions { get; set; }
        IndustryLogic Industry { get; set; }
        InsuranceLogic Insurance { get; set; }
        KillmailsLogic Killmails { get; set; }
        LocationLogic Location { get; set; }
        LoyaltyLogic Loyalty { get; set; }
        MailLogic Mail { get; set; }
        MarketLogic Market { get; set; }
        OpportunitiesLogic Opportunities { get; set; }
        PlanetaryInteractionLogic PlanetaryInteraction { get; set; }
        RoutesLogic Routes { get; set; }
        SearchLogic Search { get; set; }
        SkillsLogic Skills { get; set; }
        SovereigntyLogic Sovereignty { get; set; }
        StatusLogic Status { get; set; }
        UniverseLogic Universe { get; set; }
        UserInterfaceLogic UserInterface { get; set; }
        WalletLogic Wallet { get; set; }
        WarsLogic Wars { get; set; }

        void SetCharacterData(AuthorizedCharacterData data);
        void SetIfNoneMatchHeader(string eTag);
    }
}
