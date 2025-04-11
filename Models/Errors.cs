namespace testing.Models;

public class Errors(int missed, int overDispatched)
{
    public int Missed { get; set; } = missed;
    public int OverDispatched { get; set; } = overDispatched;
}