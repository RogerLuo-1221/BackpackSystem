using UnityEngine;

namespace BackpackSystem.Samples
{
    /// <summary>
    /// Demo 场景的启动装配脚本。挂在场景中一个空 GameObject 上。
    /// 负责:创建 Backpack、Provider、Loader,然后调用各 View 的 Init。
    /// </summary>
    public class SampleBootstrap : MonoBehaviour
    {
        [SerializeField] private BackpackPanelView _backpackPanel;
        [SerializeField] private DebugBackpackHUD _debugHud;
        [SerializeField] private string _typeDatabaseResourcesPath = "BackpackSystem/SampleItemTypeDatabase";

        private Backpack _backpack;
        private IItemTypeProvider _typeProvider;

        private void Awake()
        {
            _typeProvider = new ScriptableObjectItemTypeProvider(_typeDatabaseResourcesPath);
            IIconLoader iconLoader = new ResourcesIconLoader();
            IInstanceIdGenerator idGenerator = new SimpleIncrementalIdGenerator();

            _backpack = new Backpack(_typeProvider, idGenerator);
            _backpack.OnItemClicked += HandleItemClicked;

            if (_backpackPanel != null)
            {
                _backpackPanel.Init(_backpack, _typeProvider, iconLoader);
            }
            if (_debugHud != null)
            {
                _debugHud.Init(_backpack, _typeProvider);
            }
        }

        private void HandleItemClicked(ItemData item)
        {
            if (item == null) return;
            ItemTypeData type = _typeProvider != null ? _typeProvider.GetTypeById(item.TypeId) : null;
            string name = type != null ? type.Name : "<unknown>";
            Debug.Log($"Clicked: instanceId={item.InstanceId}, typeId={item.TypeId}, name={name}");
        }

        private void OnDestroy()
        {
            if (_backpack != null)
            {
                _backpack.OnItemClicked -= HandleItemClicked;
            }
        }
    }
}
