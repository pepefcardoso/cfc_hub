export const BrazilianState = [
  "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA",
  "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN",
  "RS", "RO", "RR", "SC", "SP", "SE", "TO"
] as const

export type BrazilianState = typeof BrazilianState[number]

export const LgpdPolicyConfig = {
  version: "1.0.0",
  text: `Política de Privacidade e Proteção de Dados (LGPD)

O Centro de Formação de Condutores (CFC) respeita a sua privacidade e garante o sigilo total das informações que você nos fornece.
Apenas dados essenciais (como CPF, nome completo, endereço) são armazenados em nosso banco de dados com o intuito de melhorar nossa relação através de e-mail, mala-direta, entre outras formas de interação.

De acordo com a Lei Geral de Proteção de Dados (LGPD - Lei 13.709/2018), os dados sensíveis coletados, incluindo dados médicos e biométricos, serão utilizados exclusivamente para os fins de matrícula e registro junto ao DETRAN.

Você tem o direito de solicitar a anonimização, bloqueio ou eliminação de dados desnecessários, excessivos ou tratados em desconformidade com as disposições da LGPD.

Ao prosseguir, você consente expressamente com a coleta e tratamento dos seus dados conforme descrito nesta política.`
}
