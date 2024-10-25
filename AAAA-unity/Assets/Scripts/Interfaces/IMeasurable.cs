using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMeasurable
{
    List<string> GetColumnNames();
    List<string> GetValues();
}
