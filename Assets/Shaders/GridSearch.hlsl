
#define FOR_EACH_NEIGHBOR(POS, GRID_DIV_NUM) {\
const uint3 cell = PositionToCell(POS);\
for (uint x = max((int)cell.x - 1, 0); x <= min(cell.x + 1, GRID_DIV_NUM.x - 1); x++) {\
for (uint y = max((int)cell.y - 1, 0); y <= min(cell.y + 1, GRID_DIV_NUM.y - 1); y++) {\
for (uint z = max((int)cell.z - 1, 0); z <= min(cell.z + 1, GRID_DIV_NUM.z - 1); z++) {\
const uint hash = x + y * GRID_DIV_NUM.x + z * GRID_DIV_NUM.x * GRID_DIV_NUM.y;\
for (uint i = _cellStartEndBuffer[hash].startIdx; i <= _cellStartEndBuffer[hash].endIdx; i++)

#define END_FOR_EACH_NEIGHBOR }}}}