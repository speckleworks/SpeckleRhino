<template>
  <v-container fluid class='layer-list pa-0'>
    <layer v-for='layer in layers' :key='layer.guid' :layer='layer' :clientId='clientId' class='layer layers-section'></layer>
  </v-container>
</template>
<script>

import Layer from './SenderLayersLayer.vue'


export default {
  name: 'SenderLayers',
  props: {
    layers: Array,
    objects: Array,
    clientId: String
  },
  components: {
    Layer
  },
  computed: {},
  data( ) {
    return {}
  },
  methods: {},
  mounted( ) {}
}
</script>
<style lang='scss'>
</style>