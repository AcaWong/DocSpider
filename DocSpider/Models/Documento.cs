using System.ComponentModel.DataAnnotations;
using System;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocSpider.Models
{
        public class Documento
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "O título é obrigatório")]
            [StringLength(100, ErrorMessage = "O título deve ter no máximo 100 caracteres")]
            [Display(Name = "Título do Documento")]
            public string Titulo { get; set; }

            [StringLength(2000, ErrorMessage = "A descrição deve ter no máximo 2000 caracteres")]
            [Display(Name = "Descrição")]
            public string Descricao { get; set; }

            [NotMapped]
            public IFormFile ArquivoUpload { get; set; }

            public string NomeArquivo { get; set; }
            public string CaminhoArquivo { get; set; }
            public DateTime DataCriacao { get; set; } = DateTime.Now;
        }

}
