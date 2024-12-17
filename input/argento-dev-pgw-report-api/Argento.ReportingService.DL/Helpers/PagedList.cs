using Argento.ReportingService.DL.Transactions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.DL.Helpers
{
    public class PagedList<T>
    {
        public int TotalUnRead { get; set; }
        public int CurrentPage { get; private set; }
        public int TotalPages { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }

        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;

        public List<T> Items { get; set; }

        public List<HeaderCoulum> Header { get; set; }

        public PagedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            Items = items;
        }

        public PagedList(List<HeaderCoulum> header, List<T> items, int count, int pageNumber, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            Items = items;
            Header = header;
        }

        public static async Task<PagedList<T>> ToPagedList(IQueryable<T> source, int pageNumber, int pageSize)
        {
            var count = source.Count();

            var query = source.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            var items = await query.ToListAsync();

            var result = new PagedList<T>(items, count, pageNumber, pageSize);
            return result;
        }

        public static async Task<PagedList<T>> ToPagedList(IQueryable<T> source, int pageNumber, int pageSize, int count)
        {
            var items = await source.ToListAsync();
            var result = new PagedList<T>(items, count, pageNumber, pageSize);
            return result;
        }

        public static async Task<PagedList<T>> ToPagedList(List<HeaderCoulum> header, List<T> source, int pageNumber, int pageSize)
        {
            var count = source.Count();

            var query = source.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            var items = query.ToList();

            var result = new PagedList<T>(header, items, count, pageNumber, pageSize);
            return result;
        }

        public static async Task<PagedList<T>> ToPagedList(List<HeaderCoulum> header, List<T> source, int pageNumber, int pageSize, int count)
        {
            var items = source.ToList();
            var result = new PagedList<T>(header, items, count, pageNumber, pageSize);
            return result;
        }

        public static async Task<PagedList<T>> Create(IEnumerable<T> source, int pageNumber, int pageSize)
        {
            var count = source.Count();
            var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            return new PagedList<T>(items, count, pageNumber, pageSize);
        }
    }
}
