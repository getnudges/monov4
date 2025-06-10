using UnAd.Models;

namespace ProductApi.Models;

public class BasicStatusType : EnumType<BasicStatus> {
    protected override void Configure(IEnumTypeDescriptor<BasicStatus> descriptor) {
        descriptor.Name("PriceTierStatus");
        descriptor.Description("The status of a price tier");
        descriptor.Value(BasicStatus.Active).Name("ACTIVE");
        descriptor.Value(BasicStatus.Inactive).Name("INACTIVE");
        descriptor.Value(BasicStatus.Archived).Name("ARCHIVED");
        descriptor.Value(BasicStatus.Deleted).Name("DELETED");
    }
}


