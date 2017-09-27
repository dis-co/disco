// This is an example of a script that can be attached to Unity game objects
// to connect them to Iris. Iris.Unity.dll (and dependencies, including FSharp.Core.dll)
// must be added to the Unity project `Assets` folder.

using UnityEngine;
using System;
using System.Collections.Generic;

public class UnitScript : MonoBehaviour {
    double scale;
    double rotationX;
    double rotationY;
    double rotationZ;

    // The same Guid must be used for all game objects
    static Guid clientGuid = new Guid("4db685e5-9b38-4413-ba4b-b04fb98a50ed");
    Iris.Unity.IIrisClient client;

    void Start () {
        Application.runInBackground = true;
        try {
            client = Iris.Unity.GetIrisClient(clientGuid, "172.16.21.169", 5000, "127.0.0.1", 3500, s => print(s));
            var values = new Dictionary<string,double>() {
                {"Scale", 0},
                {"RotationX", 0},
                {"RotationY", 0},
                {"RotationZ", 0},
            };
            client.RegisterGameObject("Unity", this.name, values, updatedValues => {
                scale = updatedValues[0];
                rotationX = updatedValues[1];
                rotationY = updatedValues[2];
                rotationZ = updatedValues[3];
            });
        }
        catch (Exception ex) {
            print(ex.Message);
        }
    }

    void Update() {
        var scale2 = (float)(1 + (scale / 100));
        transform.localScale = new Vector3(scale2, scale2, scale2);
        transform.rotation = Quaternion.Euler((float)rotationX, (float)rotationY, (float)rotationZ);
    }

    void OnDestroy() {
        if (client != null)
            client.Dispose();
    }
}