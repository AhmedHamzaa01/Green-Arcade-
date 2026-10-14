namespace RowCycle.Application.Audit;

/// <summary>Names written to <c>audit_logs.action</c>: "{entity}.{verb}".</summary>
public static class AuditActions
{
    public const string SettingsUpdate = "settings.update";

    public const string CategoryCreate = "product_category.create";
    public const string CategoryUpdate = "product_category.update";
    public const string CategoryDelete = "product_category.delete";

    public const string ProductCreate = "product.create";
    public const string ProductUpdate = "product.update";
    public const string ProductDelete = "product.delete";

    public const string VariantCreate = "product_variant.create";
    public const string VariantUpdate = "product_variant.update";
    public const string VariantDelete = "product_variant.delete";

    public const string ImageUpload = "product_image.upload";
    public const string ImageDelete = "product_image.delete";
    public const string ImageReorder = "product_image.reorder";

    public const string OrderStatusChange = "order.status";

    public const string SubmissionApprove = "submission.approve";
    public const string SubmissionReject = "submission.reject";

    public const string SubmissionCategoryCreate = "submission_category.create";
    public const string SubmissionCategoryUpdate = "submission_category.update";
    public const string SubmissionCategoryDelete = "submission_category.delete";

    public const string BadgeCreate = "badge.create";
    public const string BadgeUpdate = "badge.update";
    public const string BadgeDelete = "badge.delete";
}
