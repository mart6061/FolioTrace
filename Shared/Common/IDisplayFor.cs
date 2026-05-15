namespace AILibrary.Common;

// Contract for values that can provide display-friendly data and detail text
public interface IDisplayFor
{
    string ToData();

    string ToDetail();
}
