using UnityEngine;

namespace YogeshSriraman.DI
{
    /// <summary>
    /// Resolves dependencies for the entire scene.
    /// </summary
    public class ResolveScene : MonoBehaviour
    {
        public static ResolveScene Instance;

        private DependencyResolver dependencyResolver;

        /// <summary>
        /// Resolve scene dependencies on awake.
        /// </summary>
        void Awake()
        {
            MakeSingleTon();
            dependencyResolver = new DependencyResolver();
            dependencyResolver.ResolveScene();
        }

        public void Resolve(GameObject obj)
        {
            dependencyResolver.Resolve(obj);
        }

        #region SINGLETON
        /// <summary>
        /// Method to make sure there are no duplicates of
        /// this object in the scene.<br />
        /// Generally a good practice to define it in Awake.
        /// </summary>
        private void MakeSingleTon()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
            DontDestroyOnLoad(gameObject);  //Optional: Needed only if you want the object to be persistent
        }
        #endregion
    }
}