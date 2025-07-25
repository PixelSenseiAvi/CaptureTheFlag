//using UnityEngine;

//public class GridManager : MonoBehaviour
//{
//    public int width = 20;
//    public int height = 20;
//    public GameObject tilePrefab;
//    public GameObject obstaclePrefab;
//    public GameObject flagPrefabA;
//    public GameObject flagPrefabB;
//    public GameObject spawnPointPrefabA;
//    public GameObject spawnPointPrefabB;

//    // Example grid layout (0: empty, 1: obstacle, 2: flagA, 3: flagB, 4: spawnA, 5: spawnB)
//    int[,] gridLayout = new int[20, 20]
//    {
//        // ... fill this with your desired layout ...
//    };

//    void Start()
//    {
//        GenerateGrid();
//    }

//    void GenerateGrid()
//    {
//        for (int x = 0; x < width; x++)
//        {
//            for (int y = 0; y < height; y++)
//            {
//                Vector3 pos = new Vector3(x, 0, y);
//                Instantiate(tilePrefab, pos, Quaternion.identity, transform);

//                switch (gridLayout[x, y])
//                {
//                    case 1:
//                        Instantiate(obstaclePrefab, pos, Quaternion.identity, transform);
//                        break;
//                    case 2:
//                        Instantiate(flagPrefabA, pos, Quaternion.identity, transform);
//                        break;
//                    case 3:
//                        Instantiate(flagPrefabB, pos, Quaternion.identity, transform);
//                        break;
//                    case 4:
//                        Instantiate(spawnPointPrefabA, pos, Quaternion.identity, transform);
//                        break;
//                    case 5:
//                        Instantiate(spawnPointPrefabB, pos, Quaternion.identity, transform);
//                        break;
//                }
//            }
//        }
//    }
//}

