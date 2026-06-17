import type { Meta, StoryObj } from '@storybook/react';
import { FieldRedacted } from './FieldRedacted';

const meta = {
  title: 'Shared/FieldRedacted',
  component: FieldRedacted,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
} satisfies Meta<typeof FieldRedacted>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const InContext: Story = {
  render: () => (
    <div className="p-4 border rounded-md shadow-sm w-80 space-y-4">
      <div>
        <label className="text-sm font-medium text-gray-500">Nome do Aluno</label>
        <p className="font-semibold text-gray-900">João Silva</p>
      </div>
      <div>
        <label className="text-sm font-medium text-gray-500">CPF (Restrito)</label>
        <div>
          <FieldRedacted />
        </div>
      </div>
      <div>
        <label className="text-sm font-medium text-gray-500">Telefone (Restrito)</label>
        <div>
          <FieldRedacted />
        </div>
      </div>
    </div>
  ),
};
