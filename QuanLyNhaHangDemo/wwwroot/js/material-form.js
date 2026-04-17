$(document).ready(function () {

    const $category = $("#categoryDropdown");
    const $supplier = $("#supplierDropdown");

    // ================= INIT SELECT2 =================
    if ($supplier.length) {
        $supplier.select2({
            placeholder: "Chọn nhà cung cấp",
            width: '100%',
            allowClear: true
        });
        $supplier.prop("disabled", false).trigger("change");
    }

    if ($category.length) {
        $category.select2({
            placeholder: "Chọn danh mục",
            width: '100%'
        });
    }

    // ================= CASE 1: KHÔNG CÓ CATEGORY (EDIT) =================
    if ($category.length === 0) {
        // chỉ cần select2 supplier là đủ
        return;
    }

    // ================= CASE 2: CÓ CATEGORY (CREATE) =================
    const selectedSupplier = $supplier.data("selected") || null;
    const categoryId = $category.val();

    if (!categoryId) {
        disableSupplier();
    } else {
        loadSuppliers(categoryId, selectedSupplier);
    }

    // EVENT
    $category.on("change", function () {
        const categoryId = $(this).val();
        loadSuppliers(categoryId, null);
    });

    // ================= FUNCTIONS =================

    function disableSupplier() {
        $supplier
            .empty()
            .append('<option value="">-- Chọn nhà cung cấp --</option>')
            .prop("disabled", true)
            .trigger("change");
    }

    function enableSupplierLoading() {
        $supplier
            .prop("disabled", false)
            .empty()
            .append('<option>🔄 Đang tải...</option>')
            .trigger("change");
    }

    function loadSuppliers(categoryId, selected = null) {

        if (!categoryId) {
            disableSupplier();
            return;
        }

        enableSupplierLoading();

        $.get("/Admin/Warehouse/GetSuppliersByCategory",
            { categoryId: categoryId },
            function (data) {

                $supplier.empty();
                $supplier.append('<option value=""></option>');

                $.each(data, function (i, item) {

                    const isSelected = (selected && item.id == selected) ? "selected" : "";

                    $supplier.append(
                        `<option value="${item.id}" ${isSelected}>${item.name}</option>`
                    );
                });

                $supplier.prop("disabled", false).trigger("change");
            }
        );
    }

});