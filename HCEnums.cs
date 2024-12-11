// EVERY SINGLE ENUMERATOR THAT HAS BEEN USED... THEY NOW EXIST HERE, IN THE CONFINES OF A SINGLE PAGE.

namespace HCResourceLibraryApp
{
    /// <summary>Options that modify the returned value of <see cref="Extensions.SnippetText(string, string, string, Snip[])"/></summary>
    public enum Snip
    {
        /// <summary>Will include start and ending words in snippet.</summary>
        Inc,
        /// <summary>Will ensure that the ending word comes after starting word in snippet.</summary>
        EndAft,
        /// <summary>Will ensure that the snippet ends with the second occurence of the ending word after starting word.</summary>
        End2nd,
        /// <summary>Will ensure that the snippet ends with the last occurence of the ending word after starting word.</summary>
        EndLast,
        /// <summary>Utilizies Snippet Options: <see cref="Inc"/>, <see cref="EndAft"/>, <see cref="EndLast"/>.</summary>
        All
    }
}

namespace HCResourceLibraryApp.DataHandling
{
    /// <summary>Specifies to which category of information a <see cref="DecodeInfo"/> instance corresponds.</summary>
    public enum DecodedSection
    {
        /// <summary>Version</summary>
        Version,
        /// <summary>Added</summary>
        Added,
        /// <summary>Additional</summary>
        Additional,
        /// <summary>Total Textures Added</summary>
        TTA,
        /// <summary>Updated</summary>
        Updated,
        /// <summary>Legend</summary>
        Legend,
        /// <summary>Summary</summary>
        Summary
    }


    /// <summary>For determining the height factor of the Console Window on launch.</summary>
    public enum DimHeight
    {
        /// <summary>40% height scale.</summary>
        Squished,
        /// <summary>50% height scale.</summary>
        Short,
        /// <summary>60% height scale.</summary>
        Normal,
        /// <summary>80% height scale.</summary>
        Tall,
        /// <summary>100% height scale.</summary>
        Fill
    }
    /// <summary>For determining the width factor of the Console Window on launch.</summary>
    public enum DimWidth
    {
        /// <summary>40% width scale.</summary>
        Thin,
        /// <summary>50% width scale.</summary>
        Slim,
        /// <summary>60% width scale.</summary>
        Normal,
        /// <summary>80% width scale.</summary>
        Broad,
        /// <summary>100% width scale.</summary>
        Fill
    }


    /// <summary>Properties for Library References (Steam Formatter Usage).</summary>
    public enum LibRefProp
    {
        /// <summary>Applies to: <see cref="DecodedSection.Added"/>, <see cref="DecodedSection.Additional"/>; Fetches content's data IDs.</summary>
        Ids,
        /// <summary>Applies to: <see cref="DecodedSection.Added"/>, <see cref="DecodedSection.Additional"/>; Fetches content name (optional content name for <see cref="DecodedSection.Additional"/>).</summary>
        Name,
        /// <summary>Applies to: <see cref="DecodedSection.Additional"/>, <see cref="DecodedSection.Updated"/>; Fetches related content ID.</summary>
        RelatedID,
        /// <summary>Applies to: <see cref="DecodedSection.Additional"/>, <see cref="DecodedSection.Updated"/>; Fetches related content name.</summary>
        RelatedName,
        /// <summary>Applies only to <see cref="DecodedSection.Updated"/>; Fetches change description.</summary>
        ChangeDesc,
        /// <summary>Applies only to <see cref="DecodedSection.Legend"/>; Fetches legend key.</summary>
        Key,
        /// <summary>Applies only to <see cref="DecodedSection.Legend"/>; Fetches legend key definition.</summary>
        Definition,
        /// <summary>Applies only to <see cref="DecodedSection.Summary"/>; Fetches summary parts.</summary>
        SummaryPart
    }


    /// <summary>Icon selection for profiles</summary>
    public enum ProfileIcon
    {
        /// <summary>#0 | Default Icon. Simple semi-circle base and circle to depict a person [o(].</summary>
        StandardUserIcon, 

        /// <summary>#1 | Simple sword design.</summary>
        SwordIcon,
        /// <summary>#2 | Simple pickaxe design.</summary>
        PickaxeIcon,        
        /// <summary>#3 | Simple moutain and clouds design.</summary>
        MountainIcon,
        /// <summary>#4 | Musical eigth and quarter note design.</summary>
        MusicNoteIcon,
        /// <summary>#5 | Simple document file design.</summary>
        DocumentFileIcon,
        /// <summary>#6 | Design of something that cannot be described easily.</summary>
        AbstractIcon
    }
    /// <summary>Varying size specifics of printing the profile icon.</summary>
    public enum ProfileIconSize
    {
        /// <summary>Shrinks the profile icon to a 4x4 square print that vaguely resembles the profile icon.</summary>
        Mini, 
        /// <summary>The profile icon displayed at a normal size: 8x8 square.</summary>
        Normal,
        /// <summary>The profile icon at double the size: 16x16. The individual pixels become 2x2, no additional details are rendered for enlarged profile icon.</summary>
        Doubled
    }
    /// <summary>Specifies how to display the details of the profile.</summary>
    public enum ProfileDisplayStyle
    {
        /// <summary>Horizontal display of: profile name, profile id.</summary>
        NameAndID, 
        /// <summary>Horizontal display of: profile name, profile id, profile description.</summary>
        NoStyleInfo,
        /// <summary>Horizontal display of: profile name, profile id, profile icon style name, profile icon style colors, profile description.</summary>
        Full,
    }


    /// <summary>Specifies where information derives from within a <see cref="ResContents"/> instance. </summary>
    /// <remarks>Primarly utilized for <see cref="ResContents.ContainsDataID(string, out RCFetchSource)"/> and overloads.</remarks>
    public enum RCFetchSource
    {
        None,
        ConBaseGroup,
        ConAdditionals,
        ConChanges
    }


    /// <summary>The sources from which a relevant library search may specifically reference of matching content.</summary>
    public enum SourceCategory
    {
        /// <summary>Sourced from Base Content Name (Precedence:1)</summary>
        Bse,
        /// <summary>Sourced from Additional Related Content of a Base Content (Precedence:2)</summary>
        Adt,
        /// <summary>Sourced from Updated information of Base Content (Precedence:3)</summary>
        Upd,
    }
    /// <summary>A search option filter that allows for searches with ids only, no ids, or mixed.</summary>
    public enum SourceContents
    {
        /// <summary>Results will contain data IDs and other content information.</summary>
        All,
        /// <summary>Results will only contain data IDs.</summary>
        Ids,
        /// <summary>Results will not contain data IDs.</summary>
        NId
    }


    /// <summary>Identifies the category from which the information of a <see cref="ResLibOverwriteInfo"/> instance is sourced.</summary>
    public enum SourceOverwrite
    {
        Content, Legend, Summary
    }
   
}

namespace HCResourceLibraryApp.Layout
{
    public enum CivDisplayType
    {
        /// <summary>List every data ID, no sequential groups (no ranges). Color coding dependent on validation or invalidation.</summary>
        Expanded,
        /// <summary>List every data ID, sequential groups allowed only for validated groups. Color coding dependent on validation or invalidation.</summary>
        Compact,
        /// <summary>List only invalidated data IDs, no sequential groups (no ranges).</summary>
        Focused
    }

    /// <summary>For determining the type of item to recieve, whether file or folder.</summary>
    /// <remarks>Utilized primarily for <see cref="FileChooserPage"/>.</remarks>
    public enum FileChooserType
    {
        /// <summary>Only select file items.</summary>
        Files,
        /// <summary>Only select directory (folder) items.</summary>
        Folders,
        /// <summary>Select either directory (folder) or file items.</summary>
        All
    }


    /// <summary>Foreground Element Color. Pertains to available Console Colors by reference of usage.</summary>
    public enum ForECol
    {
        Normal,
        Highlight,
        Accent,
        Correction,
        Incorrection,
        Warning,
        Heading1,
        Heading2,
        InputColor
    }
}
