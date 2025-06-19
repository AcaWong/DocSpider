using DocSpider.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using DocSpider.Models;

namespace DocSpider.Controllers
{
    public class DocumentosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly string _uploadPath;

        public DocumentosController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _uploadPath = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(_uploadPath);
        }

        // GET: Documentos
        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["TitleSortParm"] = string.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            var documentos = from d in _context.Documentos select d;

            if (!string.IsNullOrEmpty(searchString))
            {
                documentos = documentos.Where(d => d.Titulo.Contains(searchString)
                                               || d.Descricao.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "title_desc":
                    documentos = documentos.OrderByDescending(d => d.Titulo);
                    break;
                case "Date":
                    documentos = documentos.OrderBy(d => d.DataCriacao);
                    break;
                case "date_desc":
                    documentos = documentos.OrderByDescending(d => d.DataCriacao);
                    break;
                default:
                    documentos = documentos.OrderBy(d => d.Titulo);
                    break;
            }

            return View(await documentos.AsNoTracking().ToListAsync());
        }

        // GET: Documentos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var documento = await _context.Documentos
                .FirstOrDefaultAsync(m => m.Id == id);
            if (documento == null)
            {
                return NotFound();
            }

            return View(documento);
        }

        // GET: Documentos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Documentos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Documento documento) 
        {
            if (documento.ArquivoUpload == null || documento.ArquivoUpload.Length == 0)
            {
                ModelState.AddModelError("ArquivoUpload", "Por favor, selecione um arquivo.");
                return View(documento);
            }

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt" };
            var fileExtension = Path.GetExtension(documento.ArquivoUpload.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("ArquivoUpload", "Tipo de arquivo não permitido.");
                return View(documento);
            }

            if (ModelState.IsValid)
            {
                var fileName = Guid.NewGuid().ToString() + fileExtension;
                var filePath = Path.Combine(_uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await documento.ArquivoUpload.CopyToAsync(stream);
                }

                documento.NomeArquivo = documento.ArquivoUpload.FileName;
                documento.CaminhoArquivo = fileName;
                documento.DataCriacao = DateTime.Now;

                _context.Add(documento);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(documento);
        }

        // GET: Documentos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var documento = await _context.Documentos.FindAsync(id);
            if (documento == null)
            {
                return NotFound();
            }
            return View(documento);
        }

        // POST: Documentos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Documento documento, IFormFile novoArquivo)
        {
            if (id != documento.Id)
            {
                return NotFound();
            }

            // Busca o documento atual do banco ANTES de fazer qualquer alteração
            var documentoAtual = await _context.Documentos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
            if (documentoAtual == null)
            {
                return NotFound();
            }

            string fileExtension = null;

            // Validação do arquivo primeiro
            if (novoArquivo != null && novoArquivo.Length > 0)
            {
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt" };
                fileExtension = Path.GetExtension(novoArquivo.FileName).ToLowerInvariant();

                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("novoArquivo", "Tipo de arquivo não permitido. Formatos aceitos: PDF, DOC, DOCX, XLS, XLSX, TXT");
                    return View(documento);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Se há um novo arquivo para upload
                    if (novoArquivo != null && novoArquivo.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(documentoAtual.CaminhoArquivo))
                        {
                            var oldFilePath = Path.Combine(_uploadPath, documentoAtual.CaminhoArquivo);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        // Faz upload do novo arquivo
                        var fileName = Guid.NewGuid().ToString() + fileExtension;
                        var filePath = Path.Combine(_uploadPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await novoArquivo.CopyToAsync(stream);
                        }

                        // Atualiza as informações do arquivo
                        documento.NomeArquivo = novoArquivo.FileName;
                        documento.CaminhoArquivo = fileName;
                    }
                    else
                    {
                        // MANTÉM as informações do arquivo existente
                        documento.NomeArquivo = documentoAtual.NomeArquivo;
                        documento.CaminhoArquivo = documentoAtual.CaminhoArquivo;
                    }

                    // Atualiza a data de modificação
                    _context.Update(documento);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DocumentoExists(documento.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError("", "Conflito de concorrência. O documento foi modificado por outro usuário.");
                        return View(documento);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Erro ao salvar: {ex.Message}");
                    return View(documento);
                }
            }

            return View(documento);
        }

        // GET: Documentos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var documento = await _context.Documentos
                .FirstOrDefaultAsync(m => m.Id == id);
            if (documento == null)
            {
                return NotFound();
            }

            return View(documento);
        }

        // POST: Documentos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var documento = await _context.Documentos.FindAsync(id);

            // Remove o arquivo físico
            var filePath = Path.Combine(_uploadPath, documento.CaminhoArquivo);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.Documentos.Remove(documento);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Download(int id)
        {
            var documento = await _context.Documentos.FindAsync(id);
            if (documento == null)
            {
                return NotFound();
            }

            var filePath = Path.Combine(_uploadPath, documento.CaminhoArquivo);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, GetContentType(filePath), documento.NomeArquivo);
        }

        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.ContainsKey(ext) ? types[ext] : "application/octet-stream";
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
            };
        }

        private bool DocumentoExists(int id)
        {
            return _context.Documentos.Any(e => e.Id == id);
        }
    }
}
