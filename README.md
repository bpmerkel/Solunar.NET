
# Solunar.NET

This is a .NET C# implementation of the Solunar algorithm.

# What is it?

C# implementation of Solunar values. In this case, its all in a single LINQPad file, with a NuGet reference to the excellent SunCalcNet assembly to compute Sun and Moon rising and setting times, phases, and transits.

This code is derived from Steven Musumeche's TypeScript logic: See [his repo here](https://github.com/stevenmusumeche/solunar).

# Example Usage

```cs
var date = DateTime.Today;
// Oak Landing Tide Station: Latitude 30° 15.2 N; Longitude 81° 25.8 W
var latitude = 30.2533; // Latitude (- is south)
var longitude = -81.43; // Longitude (- is west)
var s = Solunar(date, latitude, longitude);
s.Dump();   // use LINQPad's wonderful Dump() extension method to show the entire object graph
```

# Result

| Property | Value |
|----------|-------|
| DayStart | 7/14/2021 12:00:00 AM |
| DayEnd | 7/14/2021 11:59:59 PM |
| Latitude | 30.2533 |
| Longitude | -81.43 |
| Sunrise | 7/14/2021 6:34:56 AM |
| Sunset | 7/14/2021 8:30:26 PM |
| DayScore | 1 |

## Moon

| Property | Value |
|----------|-------|
| Phase | 0.13603395761331294 |
| PhaseName | Waxing Crescent |
| Illumination | 17 |
| Rise | 7/14/2021 10:50:20 AM |
| Set | 7/14/2021 11:57:44 PM |

## TransitTimes

| Type | Timestamp |
|----------|-------|
| Under | 7/14/2021 5:04:00 AM |
| Over | 7/14/2021 5:28:00 PM |

## MajorPeriods

| Type | Start | End | Weight |
|------|-------|-----|-------:|
| Major | 7/14/2021 4:04:00 AM | 7/14/2021 6:04:00 AM | 1 |
| Major | 7/14/2021 4:28:00 PM | 7/14/2021 6:28:00 PM | 0 |

## MinorPeriods

| Type | Start |End | Weight |
|------|-------|-----|-------:|
| Minor | 7/14/2021 10:20:20 AM | 7/14/2021 11:20:20 AM | 0 |
| Minor | 7/14/2021 11:27:44 PM | 7/15/2021 12:27:44 AM | 0 |

# How it works

This code calculates a Solunar score for a particular day and location, as well as major and minor feeding times based on a concept known as "Solunar Theory" by John Alden Knight. It is a hypothesis that fish and other animals move according to the location of the moon in comparison to their bodies. You can read a [more detailed description here](http://www.solunar.com/the_solunar_theory.aspx).

The key to accurate Solunar Times is the ability to chart the relative solar and lunar positions with respect to a particular location. The major periods coincide with the upper and lower meridian passage of the moon. In other words, when the moon is directly overhead and directly under foot. The minor periods occur when the moon is rising or setting on the horizon.

The Solunar scores for a day are enhanced when the major or minor periods fall within an hour or sunrise or sunset, as well as when there is a full or new moon.
