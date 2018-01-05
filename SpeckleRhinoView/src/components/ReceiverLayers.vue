<template>
  <v-container fluid class='layer-list'>
    <layer v-for='layer in layers' :key='layer.guid' :layer='layer' :clientId='clientId' class='layer layers-section'></layer>
  </v-container>
</template>

<script>
  import Layer from './ReceiverLayersLayer.vue'
  export default {
    name: 'ReceiverLayers',
    props: {
      layers: Array,
      clientId: String
    },
    components: {
      Layer
    },
    computed: {},
    data() {
      return {
      }
    },
    methods: {
    },
    mounted() {
    }
  }
</script>

<style lang='scss'>

</style>