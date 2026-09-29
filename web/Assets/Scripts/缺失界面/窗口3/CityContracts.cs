using System;
using System.Collections.Generic;

namespace Dwsg.Window3
{
    public enum CityRepairKind { Wall, Road }
    public enum CityTaxKind { Lord, Nation }

    public sealed class CityResult
    {
        public bool Success;
        public string Message;
        public static CityResult Ok(string message) { return new CityResult { Success = true, Message = message }; }
        public static CityResult Fail(string message) { return new CityResult { Message = message }; }
    }

    public sealed class CityRepairQuote
    {
        public int X, Y, Owner, PlayerId;
        public string Nation;
        public CityRepairKind Kind;
        public double Before, Amount, Copper, Food;
        public int Seconds = 30;
        public string Error;
        public bool Allowed { get { return string.IsNullOrEmpty(Error); } }
    }

    public sealed class CityScoutReport
    {
        public string City, Nation, Lord, Notice;
        public int X, Y, Fiefs;
        public double Wall, WallLimit, Road, RoadLimit;
        public bool Fighting, CanReadDefenders;
        public long ObservedUtc;
        public readonly List<string> Defenders = new List<string>();
        public string Connection = "地图观察";
    }

    // Only module state. Cities, players and treasuries stay in the existing world save.
    [Serializable]
    public sealed class CityModuleDto
    {
        public int Version = 1;
        public string WorldKey;
        public List<CityBookmark> Bookmarks = new List<CityBookmark>();
        public List<CityRepairOrder> Repairs = new List<CityRepairOrder>();
        public List<CityTaxRecord> Taxes = new List<CityTaxRecord>();
        public List<CityCandidate> Candidates = new List<CityCandidate>();
        public List<string> RecentRequests = new List<string>();
    }
    [Serializable]
    public sealed class CityBookmark { public int PlayerId, X, Y; }
    [Serializable]
    public sealed class CityRepairOrder
    {
        public int X, Y, PlayerId, Owner;
        public string Nation;
        public CityRepairKind Kind;
        public double Amount, Copper, Food;
        public long StartedUtc, EndsUtc;
    }
    [Serializable]
    public sealed class CityTaxRecord { public int X, Y; public CityTaxKind Kind; public long LastUtc; }
    [Serializable]
    public sealed class CityCandidate { public int X, Y, PlayerId; public string Nation; }

    // The default adapter is explicitly local; a future network adapter must return server results.
    public interface ICityAdapter
    {
        CityScoutReport Scout(int x, int y);
        CityRepairQuote Quote(int x, int y, CityRepairKind kind);
        CityResult Repair(CityRepairQuote quote, string request);
        CityResult Collect(int x, int y, CityTaxKind kind, string request);
        CityResult Apply(int x, int y, string request);
        CityResult Appoint(int x, int y, int playerId, string request);
        CityResult Bookmark(int x, int y, bool add);
    }
}
