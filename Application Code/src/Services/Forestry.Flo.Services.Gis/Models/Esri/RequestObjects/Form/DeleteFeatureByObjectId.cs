namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;

internal class DeleteFeatureByObjectId<T> : CommonParameters
{
    /// <summary>
    /// The features to send. Note 
    /// </summary>
    public List<T> IDs { get; set; }

    public DeleteFeatureByObjectId(List<T> ids)
    {
        IDs = ids;
    }

    /// <inheritdoc />
    public override FormUrlEncodedContent ToFormUrlEncodedContentFormData()
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("objectIds", string.Join(", ", IDs )),
            new KeyValuePair<string, string>("rollbackOnFailure", RollbackOnError.ToString()),
            new KeyValuePair<string, string>("returnDeleteResults", "false"),
            new KeyValuePair<string, string>("f", RequestFormat)
        };
        if (TokenString.HasValue) {
            data.Add(new KeyValuePair<string, string>("token", TokenString.Value));
        }
        return new FormUrlEncodedContent(data.ToArray());
    }
}
