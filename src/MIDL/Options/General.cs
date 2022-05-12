using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MIDL
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class GeneralOptions : BaseOptionPage<General> { }
    }

    public class General : BaseOptionModel<General>, IRatingConfig
    {
        [Category("My category")]
        [DisplayName("My Option")]
        [Description("An informative description.")]
        [DefaultValue(true)]
        public bool MyOption { get; set; } = true;

        [Browsable(false)]
        public int RatingRequests { get; set; }
    }
}
