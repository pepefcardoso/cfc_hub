export function formatCpf(value: string): string {
  const digits = value.replace(/\D/g, "")
  return digits
    .replace(/(\d{3})(\d)/, "$1.$2")
    .replace(/(\d{3})(\d)/, "$1.$2")
    .replace(/(\d{3})(\d{1,2})$/, "$1-$2")
    .slice(0, 14)
}

export function isValidCpf(cpf: string): boolean {
  if (typeof cpf !== "string") return false
  cpf = cpf.replace(/[^\d]+/g, "")
  if (cpf.length !== 11 || !!cpf.match(/(\d)\1{10}/)) return false

  const cpfArray = cpf.split("").map((el) => parseInt(el))
  const rest = (count: number) => {
    return (
      ((cpfArray.slice(0, count - 12).reduce((soma, el, index) => soma + el * (count - index), 0) * 10) %
        11) %
      10
    )
  }

  return rest(10) === cpfArray[9] && rest(11) === cpfArray[10]
}
