<Query Kind="Program">
  <NuGetReference>SunCalcNet</NuGetReference>
  <Namespace>SunCalcNet</Namespace>
  <Namespace>SunCalcNet.Model</Namespace>
</Query>

using static System.Math;

// Original algorithm sourced from Steven Musumeche's TypeScript code: https://github.com/stevenmusumeche/solunar

void Main()
{
	var date = DateTime.Today;
	// Oak Landing Tide Station: Latitude 30° 15.2 N; Longitude 81° 25.8 W
	var latitude = 30.2533; // Latitude (- is south)
	var longitude = -81.43; // Longitude (- is west)
	var s = Solunar(date, latitude, longitude);
	s.Dump();
	
	Enumerable.Range(0, 120)
		.Select(n => DateTime.Today.AddDays(n))
		.Select(d => Solunar(d, latitude, longitude))
		.Where(s => s.DayScore >= 4)
		.Select(s => new { s.DayStart, s.DayScore, s.Sunrise, s.Sunset, s.MajorPeriods, s.MinorPeriods })
		.Dump();
}

public SolunarInfo Solunar(DateTime date, double latitude, double longitude)
{
	var dayend = date.AddDays(1).AddSeconds(-1);
	var moondata = moonData(date, dayend, latitude, longitude);
	var sunphases = SunCalc.GetSunPhases(dayend, latitude, longitude);
	var sunrise = sunphases.First(p => p.Name.Value == SunPhaseName.Sunrise.Value).PhaseTime.ToLocalTime();
	var sunset = sunphases.First(p => p.Name.Value == SunPhaseName.Sunset.Value).PhaseTime.ToLocalTime();
	var solunarPeriods = calculateMajorMinorPeriods(moondata.TransitTimes, moondata.Rise, moondata.Set, sunrise, sunset);

	return new SolunarInfo
	{
		DayStart = date,
		DayEnd = dayend,
		Latitude = latitude,
		Longitude = longitude,
		DayScore = calculateDayScore(solunarPeriods, moondata.PhaseName, moondata.Phase),
		Sunrise = sunrise,
		Sunset = sunset,
	    Moon = new MoonInfo
		{
			Phase = moondata.Phase,
			PhaseName = moondata.PhaseName,
			Illumination = moondata.Illumination,
			Rise = moondata.Rise,
			Set = moondata.Set,
			TransitTimes = moondata.TransitTimes
		},
		MajorPeriods = solunarPeriods.Where(r => r.Type == SolunarRange.RangeType.Major).ToList(),
		MinorPeriods = solunarPeriods.Where(r => r.Type == SolunarRange.RangeType.Minor).ToList()
	};
}

private List<SolunarRange> calculateMajorMinorPeriods(List<TransitInfo> transitTimes, DateTime? moonrise, DateTime? moonset, DateTime sunrise, DateTime sunset)
{
	var ranges = transitTimes
		.Select(transitTime => new SolunarRange
		{
			Type = SolunarRange.RangeType.Major,
			Start = transitTime.Timestamp.AddMinutes(-60),
			End = transitTime.Timestamp.AddMinutes(60),
			Weight = 0
	    })
		.ToList();
	
	if (moonrise.HasValue)
	{
		ranges.Add(new SolunarRange
		{
			Type = SolunarRange.RangeType.Minor,
			Start = moonrise.Value.AddMinutes(-30),
			End = moonrise.Value.AddMinutes(30),
			Weight = 0
		});
	}

	if (moonset.HasValue)
	{
		ranges.Add(new SolunarRange
		{
			Type = SolunarRange.RangeType.Minor,
			Start = moonset.Value.AddMinutes(-30),
			End = moonset.Value.AddMinutes(30),
			Weight = 0
		});
	}

	var sunriseRange = new RangeInfo { Start = sunrise.AddMinutes(-60), End = sunrise.AddMinutes(60) };
	var sunsetRange = new RangeInfo { Start = sunset.AddMinutes(-60), End = sunset.AddMinutes(60) };
	return ranges
		.Select(r => calcWeight(r))
		.OrderBy(r => r.Start)
		.ToList();
	
	SolunarRange calcWeight(SolunarRange range)
	{
		var nearSunrise = sunriseRange.Includes(range.Start) || sunriseRange.Includes(range.End);
		var nearSunset = sunsetRange.Includes(range.Start) || sunsetRange.Includes(range.End);
		range.Weight += (nearSunrise || nearSunset) ? 1 : 0;
		return range;
	};
}

private MoonInfo moonData(DateTime start, DateTime end, double latitude, double longitude)
{
	DateTime? moonrise = null, moonset = null;
	var range = new RangeInfo { Start = start, End = end };
	for (var i = -1; i <= 1; i++)
	{
		var moonphase = MoonCalc.GetMoonPhase(start.AddDays(i), latitude, longitude);
		if (moonphase.Rise.HasValue && range.Includes(moonphase.Rise.Value))
		{
			moonrise = moonphase.Rise.Value;
		}
		if (moonphase.Set.HasValue && range.Includes(moonphase.Set.Value))
		{
			moonset = moonphase.Set.Value;
		}
	}

	var illum = MoonCalc.GetMoonIllumination(start);
	return new MoonInfo
	{
		Phase = illum.Phase,
		PhaseName = moonPhaseName(illum.Phase),
		Illumination = Round(illum.Fraction * 100),
		Rise = moonrise,
		Set = moonset,
		TransitTimes = moonTransitTimes(start, latitude, longitude)
	};
}

private List<TransitInfo> moonTransitTimes(DateTime start, double latitude, double longitude)
{
	var transitTimes = new List<TransitInfo>();
	var curSign = 0;
	for (var i = -60; i < 60 * 25; i++)
	{
		var tryingDate = start.AddMinutes(i);
		var moonPosition = MoonCalc.GetMoonPosition(tryingDate, latitude, longitude);
		if (curSign == 0) curSign = Sign(moonPosition.Azimuth);
		var tryingSign = Sign(moonPosition.Azimuth);
		if (curSign != tryingSign)
		{
			curSign = tryingSign;
			transitTimes.Add(new TransitInfo
			{
				Timestamp = tryingDate,
	        	Type = moonPosition.Altitude > 0 ? TransitInfo.TransitType.Over : TransitInfo.TransitType.Under
      		});
		}
	}
	return transitTimes
		.OrderBy(t => t.Timestamp)
		.ToList();
}

// define these magic strings as constants so we can use them in multiple places and avoid mismatches
private const string NewMoon = "New Moon";
private const string FullMoon = "Full Moon";

private int calculateDayScore(List<SolunarRange> solunarPeriods, string phaseName, double phase)
{
	var dayScore = solunarPeriods.Sum(r => r.Weight);
	if (phaseName == NewMoon || phaseName == FullMoon)
	{
		dayScore += 3;
	}
	else
	{
		if ((phase > 0.39 && phase < 0.61) || (phase + 0.5 > 0.39 && phase + 0.5 < 0.61))
		{
			dayScore += 1;
		}

		if ((phase > 0.42 && phase < 0.55) || (phase + 0.5 > 0.42 && phase + 0.5 < 0.55))
		{
			dayScore += 1;
		}
	}
	return dayScore;
}

private string moonPhaseName(double phase) => new[]
	{
		( Illumination: 0.825, Name: "Waning Crescent" ),
		( Illumination: 0.750, Name: "Last Quarter" ),
		( Illumination: 0.625, Name: "Waning Gibbous" ),
		( Illumination: 0.500, Name: FullMoon ),
		( Illumination: 0.325, Name: "Waxing Gibbous" ),
		( Illumination: 0.250, Name: "First Quarter" ),
		( Illumination: 0.125, Name: "Waxing Crescent" ),
		( Illumination: 0.000, Name: NewMoon )
	}
	.First(p => phase >= p.Illumination)
	.Name;

public class SolunarInfo
{
	public DateTime DayStart { get; set; }
	public DateTime DayEnd { get; set; }
	public double Latitude { get; set; }
	public double Longitude { get; set; }
	public DateTime Sunrise { get; set; }
	public DateTime Sunset { get; set; }
	public MoonInfo Moon { get; set; }
	public int DayScore { get; set; }
	public List<SolunarRange> MajorPeriods { get; set; }
	public List<SolunarRange> MinorPeriods { get; set; }
}

public class RangeInfo
{
	public DateTime Start { get; set; }
	public DateTime End { get; set; }
	public bool Includes(DateTime date) => date >= Start && date <= End;
}

public class MoonInfo
{
	public double Phase { get; set; }
	public string PhaseName { get; set; }
	public double Illumination { get; set; }
	public DateTime? Rise { get; set; }
	public DateTime? Set { get; set; }
	public List<TransitInfo> TransitTimes { get; set; }
}

public class SolunarRange
{
	public enum RangeType { Major, Minor }
	public RangeType Type { get; set; }
	public DateTime Start { get; set; }
	public DateTime End { get; set; }
	public int Weight { get; set; }
}

public class TransitInfo
{
	public enum TransitType { Over, Under }
	public TransitType Type { get; set; }
	public DateTime Timestamp { get; set; }
}