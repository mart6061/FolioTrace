using AILibrary.Common;
using AILibrary.Types;

namespace AILibrary.Domain;

// Marker interface for domain models
public interface IModel : IDisplayFor
{
    LastUpdatedDateTime LastUpdateDateTime { get; }
}
