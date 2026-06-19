using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

public enum CouponFrequency
{
    None = 0,
    Annual = 1,
    SemiAnnual = 2,
    Quarterly = 4,
    Monthly = 12
}
