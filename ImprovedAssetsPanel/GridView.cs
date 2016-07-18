using System;
using ColossalFramework.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ImprovedAssetsPanel
{
    public class GridView : UIPanel
    {
        private const float TOLERANCE = 0.001f;
        private UIPanel[] _assetRows;
        private UIScrollbar _scrollbar;
        int _totalItemsCount;
        private float _scrollPositionY;
        private float _maxScrollPositionY;
        public delegate UICustomControl CreateItem();
        public delegate void SetupItem(UICustomControl item, int index);

        private CreateItem _createItem;
        private SetupItem _setupItem;

        public void Initialize(UIScrollbar scrollbar, CreateItem createItem, SetupItem setupItem)
        {
            _createItem = createItem;
            _setupItem = setupItem;
            scrollbar.eventMouseUp += (component, param) =>
            {
                if (!this.isVisible)
                {
                    return;
                }

                if (Math.Abs(scrollbar.value - _scrollPositionY) < TOLERANCE)
                {
                    return;
                }
                if (RowCount <= 2)
                {
                    return;
                }

                var originalScrollPos = _scrollPositionY;
                _scrollPositionY = scrollbar.value;
                _scrollPositionY = Mathf.Clamp(_scrollPositionY, 0.0f, _maxScrollPositionY - this.size.y);

                if (Math.Abs(_scrollPositionY - originalScrollPos) < TOLERANCE)
                {
                    return;
                }
                var viewSize = this.size.y;

                var realRowIndex = (int)Mathf.Floor((_scrollPositionY / viewSize) * (viewSize / (_assetRows[0].size.y + 2.0f)));
                var diff = _scrollPositionY - realRowIndex * (_assetRows[0].size.y + 2.0f);

                var _y = 0.0f;
                for (var q = 0; q < 4; q++)
                {
                    _assetRows[q].relativePosition = new Vector3(0.0f, _y, 0.0f);
                    _y += _assetRows[q].size.y + 2.0f;
                }

                var rowsCount = RowCount;

                for (var q = 0; q < Mathf.Min(rowsCount, 4); q++)
                {
                    DrawAssets(q, realRowIndex + q);
                }

                ScrollRows(-diff);
                SetScrollBar(_scrollPositionY);
            };

            scrollbar.eventValueChanged += (component, value) =>
            {
                if (Input.GetMouseButton(0))
                {
                    return;
                }

                if (!this.isVisible)
                {
                    return;
                }

                if (Math.Abs(value - _scrollPositionY) < TOLERANCE)
                {
                    return;
                }
                if (RowCount <= 2)
                {
                    return;
                }

                var originalScrollPos = _scrollPositionY;
                _scrollPositionY = value;
                _scrollPositionY = Mathf.Clamp(_scrollPositionY, 0.0f, _maxScrollPositionY - this.size.y);

                var diff = Mathf.Clamp(_scrollPositionY - originalScrollPos, -(_assetRows[0].size.y + 4), _assetRows[0].size.y + 4);
                _scrollPositionY = originalScrollPos + diff;
                ScrollRows(-diff);
                SwapRows();

                SetScrollBar(_scrollPositionY);
            };
            _scrollbar = scrollbar;
            this.eventMouseWheel += (component, param) =>
            {
                if (RowCount <= 2)
                {
                    return;
                }

                var originalScrollPos = _scrollPositionY;
                _scrollPositionY -= param.wheelDelta * 80.0f;
                _scrollPositionY = Mathf.Clamp(_scrollPositionY, 0.0f, _maxScrollPositionY - this.size.y);

                ScrollRows(originalScrollPos - _scrollPositionY);
                SwapRows();

                SetScrollBar(_scrollPositionY);
            };

            var y = 0.0f;

            _assetRows = new UIPanel[4];
            for (var q = 0; q < 4; q++)
            {
                _assetRows[q] = parent.AddUIComponent<UIPanel>();
                _assetRows[q].name = "AssetRow" + q;
                _assetRows[q].size = new Vector2(937.0f, 173.0f);
                _assetRows[q].relativePosition = new Vector3(0.0f, y, 0.0f);
                y += _assetRows[q].size.y;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _assetRows = null;
            _totalItemsCount = 0;
        }

        public void RedrawItems(int totalItemsCount)
        {
            Clear();
            _totalItemsCount = totalItemsCount;
            var y = 0.0f;
            for (var q = 0; q < 4; q++)
            {
                _assetRows[q].relativePosition = new Vector3(0.0f, y, 0.0f);
                y += _assetRows[q].size.y + 2.0f;
            }
            _scrollPositionY = 0.0f;
            _maxScrollPositionY = RowCount * (_assetRows[0].size.y + 2.0f);
            SetScrollBar(_maxScrollPositionY, this.size.y);
            DrawAssets(0, 0);
            DrawAssets(1, 1);
            DrawAssets(2, 2);
            DrawAssets(3, 3);
        }


        private void Clear()
        {
            foreach (var row in _assetRows)
            {
                for (var i = row.components.Count - 1; i >= 0; i--)
                {
                    var child = row.components[i];
                    row.RemoveUIComponent(child);
                    Object.Destroy(child.gameObject);
                }
            }
        }

        private void ScrollRows(float yOffset)
        {
            for (var i = 0; i < 4; i++)
            {
                var row = _assetRows[i];
                row.relativePosition = new Vector3(row.relativePosition.x, row.relativePosition.y + yOffset, row.relativePosition.z);
            }
        }

        private void SwapRows()
        {
            if (_assetRows[0].relativePosition.y + _assetRows[0].size.y + 2.0f < 0.0f)
            {
                _assetRows[0].relativePosition = new Vector3(0.0f, _assetRows[3].relativePosition.y + _assetRows[3].size.y + 2.0f);
                var firstRealRow = (int)Mathf.Floor(_scrollPositionY / (_assetRows[0].size.y + 2.0f));
                var lastRealRow = firstRealRow + 3;
                DrawAssets(0, lastRealRow);
                ShiftRowsUp();
            }
            else if (_assetRows[0].relativePosition.y > 0.0f)
            {
                _assetRows[3].relativePosition = new Vector3(0.0f, _assetRows[0].relativePosition.y - _assetRows[3].size.y - 2.0f);
                var firstRealRow = (int)Mathf.Floor(_scrollPositionY / (_assetRows[0].size.y + 2.0f));
                DrawAssets(3, firstRealRow);
                ShiftRowsDown();
            }
        }

        private void ShiftRowsUp()
        {
            var tmp = _assetRows[0];
            _assetRows[0] = _assetRows[1];
            _assetRows[1] = _assetRows[2];
            _assetRows[2] = _assetRows[3];
            _assetRows[3] = tmp;
        }

        private void ShiftRowsDown()
        {
            var tmp = _assetRows[3];
            _assetRows[3] = _assetRows[2];
            _assetRows[2] = _assetRows[1];
            _assetRows[1] = _assetRows[0];
            _assetRows[0] = tmp;
        }

        private int RowCount => (int)Mathf.Ceil(_totalItemsCount / 3.0f);

        private void DrawAssets(int virtualRow, int realRow)
        {
            if (virtualRow < 0 || virtualRow > 3)
            {
                Debug.LogError("DrawAssets(): virtualRow < 0 || virtualRow > 3 is true");
                return;
            }

            var numRows = RowCount;
            if (realRow > numRows - 1)
            {
                return;
            }

            var currentPanel = _assetRows[virtualRow];
            for (var i = 0; i < currentPanel.transform.childCount; i++)
            {
                Object.Destroy(currentPanel.transform.GetChild(i).gameObject);
            }
            var panelSizeX = (float)Math.Floor(width / 3.0f);
            var panelSizeY = (float)Math.Floor(panelSizeX * 9 / 16);

            float currentX = 0;
            for (var i = realRow * 3; i < Mathf.Min((realRow + 1) * 3, _totalItemsCount); i++)
            {
                var item = _createItem();
                currentPanel.AttachUIComponent(item.gameObject);
                var panel = item.gameObject.GetComponent<UIPanel>();
                panel.size = new Vector2(panelSizeX, panelSizeY);
                panel.relativePosition = new Vector3(currentX, 0.0f);
                panel.backgroundSprite = "";
                _setupItem(item, i);
                currentX += panelSizeX;
            }
        }

        private void SetScrollBar(float maxValue, float scrollSize, float value = 0.0f)
        {
            _scrollbar.maxValue = maxValue;
            _scrollbar.scrollSize = scrollSize;
            _scrollbar.value = value;
        }

        private void SetScrollBar(float value = 0.0f)
        {
            _scrollbar.value = value;
        }
    }
}