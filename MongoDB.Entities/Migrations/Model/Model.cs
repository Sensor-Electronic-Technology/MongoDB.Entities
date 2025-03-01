using System.Collections.Generic;

namespace MongoDB.Entities;

public class Model:Entity {
    public List<Field> Fields { get; set; }
    public string CollectionName { get; set; }
}





