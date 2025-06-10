using Nudges.Models;

namespace ProductApi.Models;

public class BasicDurationType : EnumType<BasicDuration> {
    protected override void Configure(IEnumTypeDescriptor<BasicDuration> descriptor) {
        descriptor.Name("BasicDuration");
        descriptor.Description("The duration of a price tier");
        descriptor.Value(BasicDuration.Week).Name("P7D");
        descriptor.Value(BasicDuration.Month).Name("P30D");
        descriptor.Value(BasicDuration.Year).Name("P365D");
    }
}


