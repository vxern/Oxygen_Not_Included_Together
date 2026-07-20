using ONI_Together.Misc;
using Shared.OxySync;
using Shared.OxySync.Attributes;
using UnityEngine;

namespace ONI_Together.Networking.OxySync.StateMachines
{
    [SkipSaveFileSerialization]
    public class AirFilterSyncer : StateMachineSyncer
    {
        private AirFilter.StatesInstance _smi;
        private Storage _storage;

        private bool _storageDirty;
        private float _storageSyncTimer;

        private const float STORAGE_SYNC_DELAY = 0.2f;

        [SyncVar(SendMode = (int)PacketSendMode.ReliableImmediate)]
        private byte[] _storageBlob;

        private byte[] _lastAppliedStorageBlob;

        public override void OnSpawn()
        {
            base.OnSpawn();

            _smi = this.GetSMI<AirFilter.StatesInstance>();
            _storage = GetComponent<Storage>();

            if (_storage != null)
            {
                _storage.OnStorageChange += OnStorageChanged;
                _storage.Subscribe((int)GameHashes.OnStorageChange, OnStorageChangedGameHash);
                _storageDirty = true;
            }
        }

        public override void OnCleanUp()
        {
            if (_storage != null)
            {
                _storage.OnStorageChange -= OnStorageChanged;
                _storage.Unsubscribe((int)GameHashes.OnStorageChange, OnStorageChangedGameHash);
            }

            base.OnCleanUp();
        }

        private void OnStorageChanged(GameObject _)
        {
            _storageDirty = true;
        }

        private void OnStorageChangedGameHash(object _)
        {
            _storageDirty = true;
        }

        protected override int SampleCurrentStateId()
        {
            if (_smi == null || _smi.sm == null)
                return -1;

            var sm = _smi.sm;
            if (_smi.IsInsideState(sm.hasFilter.converting)) return 2;
            if (_smi.IsInsideState(sm.hasFilter.idle)) return 1;
            if (_smi.IsInsideState(sm.waiting)) return 0;
            return 0;
        }

        protected override void ApplyState(int stateId)
        {
            if (_smi == null || _smi.sm == null)
                return;

            var sm = _smi.sm;
            switch (stateId)
            {
                case 2:
                    if (!_smi.IsInsideState(sm.hasFilter.converting))
                        _smi.TryGoTo(sm.hasFilter.converting);
                    break;
                case 1:
                    if (!_smi.IsInsideState(sm.hasFilter.idle))
                        _smi.TryGoTo(sm.hasFilter.idle);
                    break;
                default:
                    if (!_smi.IsInsideState(sm.waiting))
                        _smi.TryGoTo(sm.waiting);
                    break;
            }
        }

        protected override void OnServerSampleExtra()
        {
            if (_storageDirty && _storage != null)
            {
                _storageSyncTimer += Time.unscaledDeltaTime;
                if (_storageSyncTimer >= STORAGE_SYNC_DELAY)
                {
                    _storageSyncTimer = 0f;
                    _storageDirty = false;
                    _storageBlob = BuildingUtils.EncodeStorageToBytes(_storage);
                }
            }
        }

        protected override void OnClientApplyExtra()
        {
            if (_storage != null && _storageBlob != null && _storageBlob != _lastAppliedStorageBlob)
            {
                BuildingUtils.RebuildStorageFromBytes(_storage, _storageBlob);
                _lastAppliedStorageBlob = _storageBlob;
            }
        }
    }
}
