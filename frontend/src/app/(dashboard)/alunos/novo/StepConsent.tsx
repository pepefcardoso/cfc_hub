"use client"

import * as React from "react"
import { useFormContext } from "react-hook-form"

import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { Checkbox } from "@/components/ui/checkbox"
import { LgpdPolicyConfig } from "@/lib/constants"

export function StepConsent() {
  const { control } = useFormContext()

  return (
    <div className="space-y-6">
      <div className="bg-muted p-4 rounded-md text-sm whitespace-pre-wrap text-muted-foreground h-64 overflow-y-auto border">
        {LgpdPolicyConfig.text}
      </div>

      <FormField
        control={control}
        name="consentimento"
        render={({ field }) => (
          <FormItem className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4 shadow-sm">
            <FormControl>
              <Checkbox
                checked={field.value}
                onCheckedChange={field.onChange}
              />
            </FormControl>
            <div className="space-y-1 leading-none">
              <FormLabel>
                Li e aceito a Política de Privacidade (versão {LgpdPolicyConfig.version})
              </FormLabel>
              <p className="text-sm text-muted-foreground">
                Seu consentimento é obrigatório para prosseguir com a matrícula de acordo com a LGPD.
              </p>
              <FormMessage />
            </div>
          </FormItem>
        )}
      />
    </div>
  )
}
