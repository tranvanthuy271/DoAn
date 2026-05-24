# HƯỚNG DẪN CONFIG LOADING PREFAB

## 1. Luồng mới đã đổi gì

- Loading khi đăng nhập không còn dùng `%` và progress bar nữa.
- Loading khi chuyển map, đổi khu, reconnect, server ngắt đột ngột đều dùng cùng một spinner overlay.
- Panel lỗi cũ không còn là luồng chính. Khi mất kết nối game, client sẽ hiện spinner rồi tự quay về `Login`.

## 2. Cách tạo nhanh prefab loading mới

### Cách nhanh nhất

1. Mở Unity.
2. Chạy menu `Tools > DoAn > Create Login UI Prefabs`.
3. Unity sẽ tạo lại:
   - `Assets/Prefabs/UI/LoadingPanel.prefab`
   - `Assets/Prefabs/UI/ErrorNotifyPanel.prefab`
4. `LoadingPanel.prefab` mới là dạng spinner overlay, không còn progress bar.

### Nếu muốn tự làm prefab tay trong Unity

Tạo prefab mới với cấu trúc khuyến nghị:

1. Root:
   - `RectTransform`
   - `LoadingOverlayView`
2. Child `Overlay`
   - `Image`
   - stretch full màn hình
   - màu đen alpha khoảng `0.45 -> 0.65`
3. Child `SpinnerRoot`
   - neo giữa màn hình
4. Child `SpinnerImage`
   - `Image`
   - `LoadingSpinnerAnimator`
   - size khoảng `120 -> 180`
5. Child `StatusText`
   - `TextMeshProUGUI`
   - đặt dưới spinner
   - căn giữa

Tên `SpinnerRoot`, `SpinnerImage`, `StatusText` nên giữ đúng như trên để script tự nhận.

## 3. Dùng animation hiện có của project

Project đã có sẵn frame loading ở:

- `Client/Assets/Resources/Loading`

Nếu `SpinnerImage` có component `LoadingSpinnerAnimator`, script sẽ tự lấy frame từ thư mục này và chạy vòng lặp.

Nếu anh muốn dùng animation riêng bằng `Animator` của Unity:

1. Bỏ `LoadingSpinnerAnimator`.
2. Gắn `Animator` vào `SpinnerImage` hoặc object con của nó.
3. Vẫn giữ `StatusText` nếu muốn hiện chữ như:
   - `Đang đăng nhập...`
   - `Đang chuyển map...`
   - `Đang đồng bộ nhân vật...`

## 4. Cần gán ở scene nào

### Scene `Login`

1. Mở scene `Client/Assets/Scenes/Login.unity`.
2. Chọn object `LoginLoadingManager`.
3. Ở field `loadingPanelPrefab`, kéo:
   - `Assets/Prefabs/UI/LoadingPanel.prefab`
4. Field `errorPanelPrefab` có thể để nguyên hoặc bỏ trống.

### Scene game khi test trực tiếp trong Editor

Luồng chính đi từ `Login` sang game nên `LoginLoadingManager` sẽ được `DontDestroyOnLoad`.

Nếu anh hay bấm Play trực tiếp từ `GameScene`:

1. Tạo một object rỗng trong `GameScene`.
2. Gắn `LoginLoadingManager`.
3. Gán cùng `loadingPanelPrefab`.

Làm vậy để khi chạy thẳng `GameScene` vẫn có spinner đúng chuẩn.

## 5. Những chỗ đã tự dùng spinner mới

- Đăng nhập vào game.
- Kết nối client vào game server.
- Chuyển map bằng:
  - `MapTransitionButton`
  - `MapEdgeTrigger`
  - `MapPortalTrigger`
  - `ZoneTransitionTrigger`
- Khi server ngắt hoặc client bị disconnect.
- Khi client đang chuyển scene nhưng player chưa sẵn sàng, spinner sẽ giữ tới lúc xong reposition hoặc hết thời gian chờ.

## 6. Cách test trong Unity

### Test đăng nhập

1. Vào `Login`.
2. Đăng nhập tài khoản hợp lệ.
3. Kỳ vọng:
   - Hiện spinner.
   - Không còn `%`.
   - Không còn dialog loading cũ.

### Test chuyển map

1. Dùng cổng map hoặc nút chuyển map.
2. Kỳ vọng:
   - Spinner bật ngay lúc gửi request.
   - Spinner chỉ tắt sau khi scene/map mới và nhân vật đã vào vị trí.

### Test mất kết nối

1. Vào game.
2. Tắt server game hoặc ngắt kết nối.
3. Kỳ vọng:
   - Hiện spinner.
   - Sau một nhịp ngắn tự quay về `Login`.
   - Không còn popup panel lỗi cũ.

## 7. Nếu prefab cũ vẫn đang hiện

Nguyên nhân thường là chưa tạo lại prefab hoặc chưa gán lại đúng object `LoginLoadingManager`.

Làm lại theo thứ tự:

1. Chạy `Tools > DoAn > Create Login UI Prefabs`.
2. Mở `Login.unity`.
3. Chọn `LoginLoadingManager`.
4. Kéo lại `LoadingPanel.prefab` mới vào field `loadingPanelPrefab`.
5. Save scene và test lại.
