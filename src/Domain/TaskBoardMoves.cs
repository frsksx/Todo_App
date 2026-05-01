namespace WindowsTrayTasks.Domain;

public static class TaskBoardMoves
{
    public static bool MoveHeadingNearHeading(
        Heading heading,
        Heading target,
        bool after,
        IEnumerable<Heading> headings,
        IEnumerable<TaskItem> tasks,
        out IReadOnlyList<TaskItem> movedTasks)
    {
        movedTasks = [];
        if (heading.Id == target.Id) return false;

        var targetPageId = target.PageId;
        var movedAcrossPages = heading.PageId != targetPageId;
        var orderedWithoutSource = headings
            .Where(h => h.PageId == targetPageId && h.DeletedAt is null && h.Id != heading.Id)
            .OrderBy(h => h.SortOrder)
            .ThenBy(h => h.Title)
            .ToList();
        var targetIndex = orderedWithoutSource.FindIndex(h => h.Id == target.Id);
        if (targetIndex < 0) return false;

        var previous = after
            ? orderedWithoutSource[targetIndex].SortOrder
            : targetIndex > 0 ? orderedWithoutSource[targetIndex - 1].SortOrder : (double?)null;
        var next = after
            ? targetIndex + 1 < orderedWithoutSource.Count ? orderedWithoutSource[targetIndex + 1].SortOrder : (double?)null
            : orderedWithoutSource[targetIndex].SortOrder;

        heading.PageId = targetPageId;
        heading.SortOrder = SortOrderMath.Between(previous, next);
        if (movedAcrossPages)
        {
            movedTasks = MoveHeadingTasksToPage(heading.Id, targetPageId, tasks);
        }

        return true;
    }

    public static bool MoveHeadingToPage(
        Heading heading,
        Guid pageId,
        IEnumerable<Heading> headings,
        IEnumerable<TaskItem> tasks,
        out IReadOnlyList<TaskItem> movedTasks)
    {
        heading.PageId = pageId;
        heading.SortOrder = SortOrderAtEndForPageHeadings(pageId, headings);
        movedTasks = MoveHeadingTasksToPage(heading.Id, pageId, tasks);
        return true;
    }

    public static IReadOnlyList<TaskItem> MoveHeadingTasksToPage(Guid headingId, Guid pageId, IEnumerable<TaskItem> tasks)
    {
        var moved = new List<TaskItem>();
        foreach (var task in tasks.Where(t => t.HeadingId == headingId))
        {
            task.PageId = pageId;
            moved.Add(task);
        }

        return moved;
    }

    public static IReadOnlyList<TaskItem> MoveInboxToPage(Guid sourcePageId, Guid targetPageId, IEnumerable<TaskItem> tasks)
    {
        if (sourcePageId == targetPageId) return [];

        var taskList = tasks.ToList();
        var lastSort = taskList
            .Where(t => t.PageId == targetPageId && t.ArchivedAt is null && t.HeadingId is null)
            .OrderBy(t => t.SortOrder)
            .LastOrDefault()
            ?.SortOrder;

        var moved = new List<TaskItem>();
        foreach (var task in taskList
                     .Where(t => t.PageId == sourcePageId && t.HeadingId is null)
                     .OrderBy(t => t.SortOrder)
                     .ThenBy(t => t.Title))
        {
            lastSort = SortOrderMath.Between(lastSort, null);
            task.PageId = targetPageId;
            task.SortOrder = lastSort.Value;
            moved.Add(task);
        }

        return moved;
    }

    public static double SortOrderAtEndForPageHeadings(Guid pageId, IEnumerable<Heading> headings)
        => SortOrderMath.Between(
            headings
                .Where(h => h.PageId == pageId && h.DeletedAt is null)
                .OrderBy(h => h.SortOrder)
                .LastOrDefault()
                ?.SortOrder,
            null);
}
