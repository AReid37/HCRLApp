using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCResourceLibraryApp.DataHandling
{
    /// <summary>Short-hand for "ResourceLibrary".</summary>
    public sealed class ResLibrary : DataHandlerBase
    {
        /*** RESOURCE LIBRARY
        Data form containing all information regarding resource contents, legends, and summaries from version logs
        
        Fields / Props
            - ResLib previousSelf
            - List<ResCon> contents (prv set;)
            - List<LegDt> legendData (prv set; get;)
            - List<SmryD> summaryData (prv set; get;)
            - ResCon this[int] (get -> contents[])
            - ResCon this[str] (get -> contents[] "compare by name")

        Constructors
            - ResLib()

        Methods
            - ovr EncodeToSharedFile(...)
                Also triggers individual 'EncodeToSharedFile' methods for LegendData and SummaryData at the end
            - ovr DecodeFromSharedFile(...)
                Also triggers individual 'DecodeFromSharedFile' methods for LegendData and SummaryData at the end
            - bool ChangesDetected()

        ***/
    }
}
