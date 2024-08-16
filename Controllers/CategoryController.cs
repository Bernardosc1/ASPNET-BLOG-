using Blog.Data;
using Blog.Extensions;
using Blog.Models;
using Blog.ViewModels;
using Blog.ViewModels.Categories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Data.Common;

namespace Blog.Controllers
{
    [ApiController]
    public class CategoryController : ControllerBase
    {
        // Metodo Get de categorias
        [HttpGet("v1/categories")] // Versionamento da API
        public async Task<IActionResult> GetAsync(
            [FromServices] IMemoryCache cache,
            [FromServices] BlogDataContext context)
        {
            try
            {
                var categories = cache.GetOrCreate("CategoriesCache", entry =>
                {
                    entry.SetAbsoluteExpiration(TimeSpan.FromHours(1)); // Define o tempo de expiração
                    return GetCategories(context); // Chama o método GetCategories
                });

                return Ok(new ResultViewModel<List<Category>>(categories)); // Retorna a lista de categorias
            }
            catch
            {
                return StatusCode(500, new ResultViewModel<List<Category>>("05X05 - Falha interna no servidor")); // Retorna um erro interno

            }
        }

        private List<Category> GetCategories(BlogDataContext context) // Método privado GetCategories
        {
            return context.Categories.ToList(); // Retorna a lista de categorias
        }

        // Metodo GetById de categorias
        [HttpGet("v1/categories/{id:int}")]
        public async Task<IActionResult> GetByIdAsync(
            [FromRoute] int id,
            [FromServices] BlogDataContext context)
        {
            try
            {
                var category = await context
                    .Categories
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (category == null)
                    return NotFound(new ResultViewModel<Category>("Conteúdo não encontrado"));

                return Ok(new ResultViewModel<Category>(category));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResultViewModel<Category>("05X06 - Falha interna no servidor"));

            }
        }

        // Metodo Post de categorias
        [HttpPost("v1/categories/")]
        public async Task<IActionResult> PostAsync(
           [FromBody] EditorCategoryViewModel model,
           [FromServices] BlogDataContext context)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ResultViewModel<Category>(ModelState.GetErrors()));


            try
            {
                var category = new Category
                {
                    Id = 0,
                    Name = model.Name,
                    Slug = model.Slug.ToLower(),
                };
                await context.Categories.AddAsync(category);
                await context.SaveChangesAsync();

                return Created($"v1/categories/{category.Id}", new ResultViewModel<Category>(category));
            }
            catch (DbException ex)
            {
                return StatusCode(500, new ResultViewModel<Category>("05XE9 - Não foi possivel inserir a categoria!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResultViewModel<Category>("05X10 - Falha interna no servidor"));
            }
       

        }

        // Metodo Put de categorias
        [HttpPut("v1/categories/{id:int}")]
        public async Task<IActionResult> PutAsync(
           [FromRoute] int id,
           [FromBody] EditorCategoryViewModel model,
          [FromServices] BlogDataContext context)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ResultViewModel<Category>(ModelState.GetErrors()));

            try
            {
                var category = await context.Categories.FirstOrDefaultAsync(x => x.Id == id);

                if (category == null)
                    return NotFound(new ResultViewModel<Category>("Conteúdo não encontrado"));

                category.Name = model.Name;
                category.Slug = model.Slug;

                context.Categories.Update(category);
                await context.SaveChangesAsync();

                return Ok(new ResultViewModel<Category>(category));
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new ResultViewModel<List<Category>>("05XE8 - Não foi possivel alterar a categoria!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResultViewModel<Category>("05X11 - Falha interna no servidor"));

            }
        }

        // Metodo Delete de categorias
        [HttpDelete("v1/categories/{id:int}")]
        public async Task<IActionResult> DeleteAsync(
          [FromRoute] int id,
         [FromServices] BlogDataContext context)
        {
            try
            {
                var category = await context
                    .Categories
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (category == null)
                    return NotFound(new ResultViewModel<Category>("Conteúdo não encontrado"));

                context.Categories.Remove(category);
                await context.SaveChangesAsync();

                return Ok(new ResultViewModel<Category>(category));
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new ResultViewModel<Category>("05XE7 - Não foi possivel deletar a categoria!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResultViewModel<Category>("05X12 - Falha interna no servidor"));

            }
        }
    }
}