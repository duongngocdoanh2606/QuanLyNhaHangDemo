@using QuanLyNhaHangDemo.Helpers
@model IEnumerable<OrderDetailsModel>

<table class="table table-sm">
    <thead>
        <tr>
            <th>Món</th>
            <th>Thời gian đặt</th>
            <th>SL</th>
            <th>Trạng thái</th>
            <th></th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in Model)
        {
            <tr class="@(item.FiredAt.HasValue ? "table-danger" : "")">
                <td>
                    @item.Product.Name
                    @if (item.FireCount > 0)
                    {
                        <span class="badge bg-warning text-dark">Làm lại ×@item.FireCount</span>
                    }
                    @if (item.IsFired)
                    {
                        <span class="badge bg-danger"><i class="fas fa-fire"></i> FIRE</span>
                    }
                </td>
                <td>@item.CreateDate</td>
                <td>@item.Quantity</td>
                <td>
                    @OrderStatusHelper.GetKitchenStatusLabel(item.Status)
                </td>
                <td>
                    <form asp-area="Admin"
                          asp-controller="Kitchen"
                          asp-action="UpdateStatus"
                          method="post" class="form-inline">
                        @Html.AntiForgeryToken()

                        <input type="hidden" name="orderDetailId" value="@item.Id" />
                        <input type="hidden" name="returnAction"
                               value="@ViewContext.RouteData.Values["action"]" />

                        <select name="status" class="form-select form-select-sm">
                            <option value="0" selected="@(item.Status == StatusProduct.Pending)">Hold / Pending</option>
                            <option value="1" selected="@(item.Status == StatusProduct.PreparingIngredient)">Preparing Ingredient</option>
                            <option value="2" selected="@(item.Status == StatusProduct.Cooking)">Cooking</option>
                            <option value="3" selected="@(item.Status == StatusProduct.Served)">Served</option>
                        </select>

                        <button type="submit" class="btn btn-sm btn-primary mt-1">Lưu</button>
                    </form>
                </td>
            </tr>
        }
    </tbody>
</table>