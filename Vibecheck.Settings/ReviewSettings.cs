namespace Vibecheck.Settings;

public class ReviewSettings
{
    /// <summary>
    /// Some dumb AIs can't figure out what is the old code and what is the new code. Help them not get confused.
    /// </summary>
    public bool OnlyNewCode { get; set; } = true;
}
