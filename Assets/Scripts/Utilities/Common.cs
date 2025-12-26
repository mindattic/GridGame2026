using Assets.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global numeric constants used across the game.
/// </summary>
public static class Common
{
    public const int MaxPartyMemberCount = 6;
}

public static class TextSymbol
{
    public const string Infinity = "\u221E";         
    public const string Bullet = "\u2022";             
    public const string Ellipsis = "\u2026";           
    public const string EnDash = "\u2013";             
    public const string EmDash = "\u2014";             
    public const string LeftDoubleQuote = "\u201C";    
    public const string RightDoubleQuote = "\u201D";   
    public const string LeftSingleQuote = "\u2018";    
    public const string RightSingleQuote = "\u2019";   
    public const string Trademark = "\u2122";          
    public const string Copyright = "\u00A9";          
    public const string Registered = "\u00AE";         
}

/// <summary>
/// Standard date format strings.
/// </summary>
public static class DateFormat
{
    public const string yyyyMMddHHmmss = "yyyy.MM.dd.HH.mm.ss";
}

/// <summary>
/// Common percentage and fractional values for calculations.
/// </summary>
public static class Increment
{
    // Common Percent Constants
    public const float Percent1 = 0.01f;
    public const float Percent2 = 0.02f;
    public const float Percent3 = 0.03f;
    public const float Percent4 = 0.04f;
    public const float Percent5 = 0.05f;
    public const float Percent6 = 0.06f;
    public const float Percent7 = 0.07f;
    public const float Percent8 = 0.08f;
    public const float Percent9 = 0.09f;
    public const float Percent10 = 0.10f;
    public const float Percent16 = 0.16666667f;
    public const float Percent20 = 0.20f;
    public const float Percent25 = 0.25f;
    public const float Percent30 = 0.30f;
    public const float Percent33 = 0.33333334f;
    public const float Percent40 = 0.40f;
    public const float Percent50 = 0.50f;
    public const float Percent60 = 0.60f;
    public const float Percent66 = 0.6666667f;
    public const float Percent70 = 0.70f;
    public const float Percent75 = 0.75f;
    public const float Percent80 = 0.80f;
    public const float Percent90 = 0.90f;
    public const float Percent100 = 1.0f;

    // Opacity Values
    public const float Opaque = 1f;
    public const float Transparent = 0f;

    // Common fractional constants 
    public const float Half = 0.5f;
    public const float OneThird = 0.33333334f;
    public const float OneFourth = 0.25f;
    public const float OneFifth = 0.20f;
    public const float OneSixth = 0.16666667f;
    public const float OneSeventh = 0.14285715f;
    public const float OneEighth = 0.125f;
    public const float OneNinth = 0.11111111f;

    public static class HealthBar
    {
        public const float Drain = 1.0f;
    }

    public static class ActionBar
    {
        public const float Drain = 1.0f;
    }
}

/// <summary>
/// Timings for intermission stages before and after actions.
/// </summary>
public static class Intermission
{
    public static class Before
    {
        public static class Enemy
        {
            public static float Move = 0;
            public static float Attack = 0;
        }

        public static class Player
        {
            public static float Attack = 0;
        }

        public static class Portrait
        {
            public static float SlideIn = 0;
        }

        public static class HealthBar
        {
            public static float Drain = Interval.QuarterSecond;
        }

        public static class ActionBar
        {
            public static float Drain = 0;
        }
    }

    public static class After
    {
        public static class Player
        {
            public static float Attack = 0;
        }

        public static class HealthBar
        {
            public static float Empty = 0;
        }
    }
}

/// <summary>
/// Common time intervals.
/// </summary>
public static class Interval
{
    public const float OneTick = 0.01f;
    public const float FiveTicks = 0.05f;
    public const float TenTicks = 0.10f;
    public const float TenthSecond = 0.10f;
    public const float QuarterSecond = 0.25f;
    public const float HalfSecond = 0.50f;
    public const float OneSecond = 1.0f;
    public const float TwoSeconds = 2.0f;
    public const float ThreeSeconds = 3.0f;
    public const float FourSeconds = 4.0f;
    public const float FiveSeconds = 5.0f;
}

/// <summary>
/// Standard opacity levels and byte-based alpha equivalents.
/// </summary>
public static class Opacity
{
    public const float Opaque = 1f;
    public const float Percent90 = 0.90f;
    public const float Percent80 = 0.80f;
    public const float Percent70 = 0.70f;
    public const float Percent60 = 0.60f;
    public const float Percent50 = 0.50f;
    public const float Percent40 = 0.40f;
    public const float Percent30 = 0.30f;
    public const float Percent20 = 0.20f;
    public const float Percent10 = 0.10f;
    public const float Transparent = 0f;

    public static class Translucent
    {
        public const float Alpha196 = 0.76862745f;
        public const float Alpha128 = 0.50196078f;
        public const float Alpha64 = 0.25098039f;
        public const float Alpha32 = 0.12549020f;
    }
}

/// <summary>
/// Item rarity definitions with associated colors.
/// </summary>
public static class Rarities
{
    public static Rarity Junk = new Rarity("Junk", ColorHelper.RGB(128, 128, 128));
    public static Rarity Common = new Rarity("Common", ColorHelper.RGB(255, 255, 255));
    public static Rarity Uncommon = new Rarity("Uncommon", ColorHelper.RGB(30, 255, 0));
    public static Rarity Rare = new Rarity("Rare", ColorHelper.RGB(0, 112, 221));
    public static Rarity Epic = new Rarity("Epic", ColorHelper.RGB(163, 53, 238));
    public static Rarity Legendary = new Rarity("Legendary", ColorHelper.RGB(255, 128, 0));
}

/// <summary>
/// Common WaitForSeconds helpers for coroutines.
/// </summary>
public static class Wait
{
    public static WaitForSeconds OneTick() => new WaitForSeconds(Interval.OneTick);
    public static WaitForSeconds Ticks(int amount) => new WaitForSeconds(Interval.OneTick * amount);
    public static WaitForSeconds For(float seconds) => new WaitForSeconds(seconds);

    //Immediate completion enumerator for coroutines that need to yield but have nothing to wait for.
    public static object None() => Immediate.Instance;

    // Wait until end of frame
    public static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();
    public static object UntilEndOfFrame() => EndOfFrame;

    // Wait until next frame (equivalent to yield return null)
    public static object UntilNextFrame() => null;

    // Singleton no-op IEnumerator (MoveNext returns false immediately).
    private sealed class Immediate : IEnumerator
    {
        public static readonly Immediate Instance = new Immediate();
        public bool MoveNext() => false;
        public void Reset() { }
        public object Current => null;
    }
}
