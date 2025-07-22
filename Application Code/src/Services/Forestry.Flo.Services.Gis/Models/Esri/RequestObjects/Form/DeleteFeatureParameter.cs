namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;

public class DeleteFeatureParameter : CommonParameters
{
    /// <summary>
    /// The list of Ids to remove from the layer
    /// </summary>
    public List<int> IDs { get; set; }

    public DeleteFeatureParameter(List<int> ids)
    {
        IDs = ids;
    }

    /// <inheritdoc />
    public override FormUrlEncodedContent ToFormUrlEncodedContentFormData()
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("features", string.Join(", ", IDs )),
            new KeyValuePair<string, string>("rollbackOnFailure", RollbackOnError.ToString()),
            new KeyValuePair<string, string>("gdbVersion", ""),
            new KeyValuePair<string, string>("f", RequestFormat)
        };
        if (TokenString.HasValue)
        {
            data.Add(new KeyValuePair<string, string>("token", TokenString.Value));
        }
        return new FormUrlEncodedContent(data.ToArray());
    }
}
